using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Models.ViewModels;

using HWSETA_Impact_Hub.Models.ViewModels.Chats;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HWSETA_Impact_Hub.Services.Implementations
{
    public sealed class ChatService : IChatService
    {
        private readonly ApplicationDbContext _db;

        public ChatService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<AdminChatProfileOptionVm>> GetAdminChatProfilesAsync(CancellationToken ct)
        {
            return await _db.AdminChatProfiles
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayName)
                .Select(x => new AdminChatProfileOptionVm
                {
                    Id = x.Id,
                    DisplayName = x.DisplayName,
                    AvatarColor = x.AvatarColor
                })
                .ToListAsync(ct);
        }

        public async Task<ChatInboxVm> GetInboxAsync(CancellationToken ct)
        {
            var rows = await _db.ConversationThreads
                .AsNoTracking()
                .Include(x => x.Beneficiary)
                .Include(x => x.Messages)
                .OrderByDescending(x => x.LastMessageOnUtc)
                .ToListAsync(ct);

            var vm = new ChatInboxVm
            {
                Threads = rows.Select(x =>
                {
                    var last = x.Messages
                        .OrderByDescending(m => m.SentOnUtc)
                        .FirstOrDefault();

                    return new ChatInboxItemVm
                    {
                        ThreadId = x.Id,
                        BeneficiaryId = x.BeneficiaryId,
                        BeneficiaryName = $"{x.Beneficiary.FirstName ?? ""} {x.Beneficiary.LastName ?? ""}".Trim(),
                        Subject = x.Subject,
                        LastMessagePreview = last?.MessageText ?? "",
                        LastSender = last?.SenderDisplayName ?? "",
                        LastMessageOnUtc = x.LastMessageOnUtc,
                        Status = x.Status.ToString(),
                        HasUnreadAdminMessage = x.HasUnreadAdminMessage,
                        HasUnreadBeneficiaryMessage = x.HasUnreadBeneficiaryMessage
                    };
                }).ToList()
            };

            return vm;
        }

        public async Task<CreateThreadVm> BuildCreateVmAsync(CancellationToken ct)
        {
            var beneficiaries = await _db.Beneficiaries
                .AsNoTracking()
                .OrderBy(x => x.LastName)
                .ThenBy(x => x.FirstName)
                .Select(x => new
                {
                    x.Id,
                    x.FirstName,
                    x.LastName,
                    x.IdentifierValue
                })
                .ToListAsync(ct);

            var chatProfiles = await _db.AdminChatProfiles
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayName)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.DisplayName
                })
                .ToListAsync(ct);

            var publishedForms = await _db.FormPublishes
                .AsNoTracking()
                .Include(x => x.FormTemplate)
                .OrderByDescending(x => x.CreatedOnUtc)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.FormTemplate.Title + " (v" + x.FormTemplate.Version + ")"
                })
                .ToListAsync(ct);

            return new CreateThreadVm
            {
                Beneficiaries = beneficiaries
                    .Select(x => new SelectListItem
                    {
                        Value = x.Id.ToString(),
                        Text = $"{(x.LastName ?? "").Trim()}, {(x.FirstName ?? "").Trim()} - {x.IdentifierValue}"
                    })
                    .ToList(),
                AdminChatProfiles = chatProfiles,
                PublishedForms = publishedForms
            };
        }

        public async Task<(bool ok, string? error, Guid? threadId)> CreateThreadAsync(
            CreateThreadVm vm,
            string currentUserId,
            CancellationToken ct)
        {
            var beneficiaryExists = await _db.Beneficiaries
                .AsNoTracking()
                .AnyAsync(x => x.Id == vm.BeneficiaryId, ct);

            if (!beneficiaryExists)
                return (false, "Selected beneficiary was not found.", null);

            var chatProfile = await _db.AdminChatProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == vm.AdminChatProfileId && x.IsActive, ct);

            if (chatProfile == null)
                return (false, "Select a valid chatter name.", null);

            var subject = (vm.Subject ?? "").Trim();
            if (string.IsNullOrWhiteSpace(subject))
                return (false, "Subject is required.", null);

            var utcNow = DateTime.UtcNow;

            var thread = new ConversationThread
            {
                BeneficiaryId = vm.BeneficiaryId,
                Subject = subject,
                Status = ConversationThreadStatus.Open,
                HasUnreadAdminMessage = false,
                HasUnreadBeneficiaryMessage = true,
                LastMessageOnUtc = utcNow,
                CreatedOnUtc = utcNow,
                CreatedByUserId = currentUserId
            };

            _db.ConversationThreads.Add(thread);
            await _db.SaveChangesAsync(ct);

            var hasTextMessage = !string.IsNullOrWhiteSpace(vm.MessageText);
            if (hasTextMessage)
            {
                var msg = new ConversationMessage
                {
                    ThreadId = thread.Id,
                    BeneficiaryId = vm.BeneficiaryId,
                    SenderType = ConversationSenderType.Admin,
                    SenderUserId = currentUserId,
                    AdminChatProfileId = chatProfile.Id,
                    SenderDisplayName = chatProfile.DisplayName,
                    MessageText = vm.MessageText.Trim(),
                    IsRead = false,
                    SentOnUtc = utcNow,
                    CreatedOnUtc = utcNow,
                    CreatedByUserId = currentUserId
                };

                _db.ConversationMessages.Add(msg);
            }

            await _db.SaveChangesAsync(ct);

            if (vm.FormPublishId.HasValue)
            {
                var (formOk, formErr) = await SendFormAsync(
                    thread.Id,
                    vm.AdminChatProfileId,
                    vm.FormPublishId,
                    vm.FormNote,
                    currentUserId,
                    ct);

                if (!formOk)
                    return (false, formErr ?? "Thread created, but form could not be attached.", thread.Id);
            }

            return (true, null, thread.Id);
        }

        public async Task<ChatThreadVm?> GetThreadAsync(Guid threadId, string currentUserId, CancellationToken ct)
        {
            var thread = await _db.ConversationThreads
                .Include(x => x.Beneficiary)
                .Include(x => x.Messages.OrderBy(m => m.SentOnUtc))
                    .ThenInclude(m => m.FormPublish)
                .FirstOrDefaultAsync(x => x.Id == threadId, ct);

            if (thread == null) return null;

            thread.HasUnreadAdminMessage = false;

            foreach (var msg in thread.Messages.Where(x => x.SenderType == ConversationSenderType.Beneficiary && !x.IsRead))
                msg.IsRead = true;

            await _db.SaveChangesAsync(ct);

            var chatProfiles = await _db.AdminChatProfiles
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayName)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.DisplayName
                })
                .ToListAsync(ct);

            var publishedForms = await _db.FormPublishes
                .AsNoTracking()
                .Include(x => x.FormTemplate)
                .OrderByDescending(x => x.CreatedOnUtc)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.FormTemplate.Title + " (v" + x.FormTemplate.Version + ")"
                })
                .ToListAsync(ct);

            return new ChatThreadVm
            {
                ThreadId = thread.Id,
                BeneficiaryId = thread.BeneficiaryId,
                BeneficiaryName = $"{thread.Beneficiary.FirstName ?? ""} {thread.Beneficiary.LastName ?? ""}".Trim(),
                Subject = thread.Subject,
                Status = thread.Status.ToString(),
                AdminChatProfiles = chatProfiles,
                Messages = thread.Messages.Select(m => new ChatMessageVm
                {
                    MessageId = m.Id,
                    SenderDisplayName = m.SenderDisplayName,
                    SenderType = m.SenderType.ToString(),
                    MessageText = m.MessageText,
                    SentOnUtc = m.SentOnUtc,
                    IsMine = m.SenderType == ConversationSenderType.Admin,
                    IsFormShareMessage = m.IsFormShareMessage,
                    FormPublishId = m.FormPublishId,
                    BeneficiaryFormInviteId = m.BeneficiaryFormInviteId,
                    FormTitle = m.FormPublish != null && m.FormPublish.FormTemplate != null
                        ? m.FormPublish.FormTemplate.Title
                        : null
                }).ToList(),
                SendForm = new SendThreadFormVm
                {
                    ThreadId = thread.Id,
                    AdminChatProfiles = chatProfiles,
                    PublishedForms = publishedForms
                }
            };
        }

        public async Task<(bool ok, string? error)> ReplyAsync(
            Guid threadId,
            string replyText,
            Guid? adminChatProfileId,
            string currentUserId,
            CancellationToken ct)
        {
            var thread = await _db.ConversationThreads
                .FirstOrDefaultAsync(x => x.Id == threadId, ct);

            if (thread == null)
                return (false, "Conversation thread not found.");

            if (thread.Status == ConversationThreadStatus.Closed)
                return (false, "This conversation is closed.");

            var text = (replyText ?? "").Trim();
            if (string.IsNullOrWhiteSpace(text))
                return (false, "Reply message is required.");

            var chatProfile = await _db.AdminChatProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == adminChatProfileId && x.IsActive, ct);

            if (chatProfile == null)
                return (false, "Select a valid chatter name.");

            var utcNow = DateTime.UtcNow;

            var msg = new ConversationMessage
            {
                ThreadId = thread.Id,
                BeneficiaryId = thread.BeneficiaryId,
                SenderType = ConversationSenderType.Admin,
                SenderUserId = currentUserId,
                AdminChatProfileId = chatProfile.Id,
                SenderDisplayName = chatProfile.DisplayName,
                MessageText = text,
                IsRead = false,
                SentOnUtc = utcNow,
                CreatedOnUtc = utcNow,
                CreatedByUserId = currentUserId
            };

            thread.LastMessageOnUtc = utcNow;
            thread.HasUnreadBeneficiaryMessage = true;
            thread.HasUnreadAdminMessage = false;
            thread.UpdatedOnUtc = utcNow;
            thread.UpdatedByUserId = currentUserId;

            _db.ConversationMessages.Add(msg);
            await _db.SaveChangesAsync(ct);

            return (true, null);
        }

        public async Task<(bool ok, string? error)> SendFormAsync(
            Guid threadId,
            Guid? adminChatProfileId,
            Guid? formPublishId,
            string? note,
            string currentUserId,
            CancellationToken ct)
        {
            var thread = await _db.ConversationThreads
                .FirstOrDefaultAsync(x => x.Id == threadId, ct);

            if (thread == null)
                return (false, "Conversation thread not found.");

            if (thread.Status == ConversationThreadStatus.Closed)
                return (false, "This conversation is closed.");

            var chatProfile = await _db.AdminChatProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == adminChatProfileId && x.IsActive, ct);

            if (chatProfile == null)
                return (false, "Select a valid chatter name.");

            var formPublish = await _db.FormPublishes
                .Include(x => x.FormTemplate)
                .FirstOrDefaultAsync(x => x.Id == formPublishId, ct);

            if (formPublish == null)
                return (false, "Selected published form was not found.");

            var utcNow = DateTime.UtcNow;
            var inviteToken = Guid.NewGuid().ToString("N");

            var invite = new BeneficiaryFormInvite
            {
                BeneficiaryId = thread.BeneficiaryId,
                FormPublishId = formPublish.Id,
                Channel = InviteChannel.Chat,
                InviteToken = inviteToken,
                Status = "Sent",
                SentOnUtc = utcNow,
                ExpiryOnUtc = utcNow.AddDays(14),
                CreatedOnUtc = utcNow,
                CreatedByUserId = currentUserId
            };

            _db.BeneficiaryFormInvites.Add(invite);
            await _db.SaveChangesAsync(ct);

            var title = formPublish.FormTemplate?.Title ?? "Form";

            var messageText = string.IsNullOrWhiteSpace(note)
                ? $"{chatProfile.DisplayName} shared a form with you: {title}"
                : $"{chatProfile.DisplayName} shared a form with you: {title}\n\nNote: {note.Trim()}";

            var msg = new ConversationMessage
            {
                ThreadId = thread.Id,
                BeneficiaryId = thread.BeneficiaryId,
                SenderType = ConversationSenderType.Admin,
                SenderUserId = currentUserId,
                AdminChatProfileId = chatProfile.Id,
                SenderDisplayName = chatProfile.DisplayName,
                MessageText = messageText,
                IsRead = false,
                SentOnUtc = utcNow,
                CreatedOnUtc = utcNow,
                CreatedByUserId = currentUserId,
                IsFormShareMessage = true,
                FormPublishId = formPublish.Id,
                BeneficiaryFormInviteId = invite.Id
            };

            thread.LastMessageOnUtc = utcNow;
            thread.HasUnreadBeneficiaryMessage = true;
            thread.HasUnreadAdminMessage = false;
            thread.UpdatedOnUtc = utcNow;
            thread.UpdatedByUserId = currentUserId;

            _db.ConversationMessages.Add(msg);
            await _db.SaveChangesAsync(ct);

            return (true, null);
        }

        public async Task<(bool ok, string? error)> CloseThreadAsync(Guid threadId, CancellationToken ct)
        {
            var thread = await _db.ConversationThreads.FirstOrDefaultAsync(x => x.Id == threadId, ct);
            if (thread == null)
                return (false, "Conversation thread not found.");

            thread.Status = ConversationThreadStatus.Closed;
            thread.UpdatedOnUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return (true, null);
        }
    }
}