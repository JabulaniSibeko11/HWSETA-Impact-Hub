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

            return new CreateThreadVm
            {
                Beneficiaries = beneficiaries
                    .Select(x => new SelectListItem
                    {
                        Value = x.Id.ToString(),
                        Text = $"{(x.LastName ?? "").Trim()}, {(x.FirstName ?? "").Trim()} - {x.IdentifierValue}"
                    })
                    .ToList(),
                AdminChatProfiles = chatProfiles
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

            var messageText = (vm.MessageText ?? "").Trim();
            if (string.IsNullOrWhiteSpace(messageText))
                return (false, "Message is required.", null);

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

            var msg = new ConversationMessage
            {
                Thread = thread,
                BeneficiaryId = vm.BeneficiaryId,
                SenderType = ConversationSenderType.Admin,
                SenderUserId = currentUserId,
                AdminChatProfileId = chatProfile.Id,
                SenderDisplayName = chatProfile.DisplayName,
                MessageText = messageText,
                IsRead = false,
                SentOnUtc = utcNow,
                CreatedOnUtc = utcNow,
                CreatedByUserId = currentUserId
            };

            _db.ConversationThreads.Add(thread);
            _db.ConversationMessages.Add(msg);

            await _db.SaveChangesAsync(ct);
            return (true, null, thread.Id);
        }

        public async Task<ChatThreadVm?> GetThreadAsync(Guid threadId, string currentUserId, CancellationToken ct)
        {
            var thread = await _db.ConversationThreads
                .Include(x => x.Beneficiary)
                .Include(x => x.Messages.OrderBy(m => m.SentOnUtc))
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
                    IsMine = m.SenderType == ConversationSenderType.Admin
                }).ToList()
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