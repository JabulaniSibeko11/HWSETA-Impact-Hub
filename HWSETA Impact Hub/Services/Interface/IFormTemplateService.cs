using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Models.ViewModels.Forms;

namespace HWSETA_Impact_Hub.Services.Interface
{
    public interface IFormTemplateService
    {
        Task<List<FormTemplateListRowVm>> ListAsync(CancellationToken ct);
        Task<FormTemplate?> GetEntityAsync(Guid id, CancellationToken ct);

        Task<(bool ok, string? error, Guid? id)> CreateAsync(FormTemplateCreateVm vm, CancellationToken ct);
        Task<(bool ok, string? error)> UpdateAsync(FormTemplateEditVm vm, CancellationToken ct);

        Task<FormTemplateBuilderVm?> GetBuilderAsync(Guid templateId, CancellationToken ct);

        Task<(bool ok, string? error, Guid? sectionId)> AddSectionAsync(FormSectionCreateVm vm, CancellationToken ct);
        Task<(bool ok, string? error)> UpdateSectionAsync(FormSectionEditVm vm, CancellationToken ct);
        Task<(bool ok, string? error)> DeleteSectionAsync(Guid sectionId, CancellationToken ct);
        Task<(bool ok, string? error)> ReorderSectionsAsync(Guid templateId, List<Guid> orderedSectionIds, CancellationToken ct);

        Task<(bool ok, string? error, Guid? fieldId)> AddFieldAsync(FormFieldCreateVm vm, CancellationToken ct);
        Task<(bool ok, string? error)> UpdateFieldAsync(FormFieldEditVm vm, CancellationToken ct);
        Task<(bool ok, string? error)> DeleteFieldAsync(Guid fieldId, CancellationToken ct);
        Task<(bool ok, string? error)> ReorderFieldsAsync(Guid sectionId, List<Guid> orderedFieldIds, CancellationToken ct);

        Task<(bool ok, string? error, Guid? optionId)> AddOptionAsync(FormFieldOptionCreateVm vm, CancellationToken ct);
        Task<(bool ok, string? error)> UpdateOptionAsync(FormFieldOptionEditVm vm, CancellationToken ct);
        Task<(bool ok, string? error)> DeleteOptionAsync(Guid optionId, CancellationToken ct);
        Task<(bool ok, string? error)> ReorderOptionsAsync(Guid fieldId, List<Guid> orderedOptionIds, CancellationToken ct);


        Task<FormPublishVm?> GetPublishVmAsync(Guid templateId, string baseUrl, CancellationToken ct);
        Task<(bool ok, string? error)> PublishAsync(FormPublishVm vm, CancellationToken ct);
        Task<(bool ok, string? error)> UnpublishAsync(Guid templateId, CancellationToken ct);

        Task<PublicFormVm?> GetPublicFormAsync(string token, string? prefillEmail, string? prefillPhone, CancellationToken ct);

        
        Task<(bool ok, string? error, Guid? submissionId, string? nextUrl)> SubmitPublicAsync(PublicFormSubmitVm vm, string? ip, string? userAgent, CancellationToken ct);

        Task<(bool ok, string? error)> DeleteAsync(Guid templateId, CancellationToken ct);


    }
}
