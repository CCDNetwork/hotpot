using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Ccd.Server.AdministrativeRegions;
using Ccd.Server.Areas.Referrals.Helpers;
using Ccd.Server.Data;
using Ccd.Server.Helpers;
using Ccd.Server.Organizations;
using Ccd.Server.Storage;
using Ccd.Server.Users;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace Ccd.Server.Referrals;

public class ReferralService
{
    private readonly AdministrativeRegionService _administrativeRegionService;
    private readonly CcdContext _context;
    private readonly IMapper _mapper;
    private readonly OrganizationService _organizationService;

    private readonly string _selectSql =
        @"
             SELECT DISTINCT ON (r.id)
                 r.*
             FROM
                 referral r
             WHERE
                (CASE
                    WHEN @received THEN organization_referred_to_id = @organizationId
                    ELSE organization_created_id = @organizationId
                END)
                AND (@id is null OR r.id = @id)";

    private readonly IStorageService _storageService;
    private readonly UserService _userService;

    public ReferralService(
        IMapper mapper,
        CcdContext context,
        IStorageService storageService,
        UserService userService,
        OrganizationService organizationService,
        AdministrativeRegionService administrativeRegionService
    )
    {
        _mapper = mapper;
        _context = context;
        _storageService = storageService;
        _userService = userService;
        _organizationService = organizationService;
        _administrativeRegionService = administrativeRegionService;
    }

    private object getSelectSqlParams(
        Guid? id = null,
        Guid? organizationId = null,
        bool received = false
    )
    {
        return new
        {
            id,
            organizationId,
            received
        };
    }

    public async Task<PagedApiResponse<ReferralResponse>> GetReferralsApi(
        Guid organizationId,
        RequestParameters requestParameters = null,
        bool received = false
    )
    {
        return await PagedApiResponse<ReferralResponse>.GetFromSql(
            _context,
            _selectSql,
            getSelectSqlParams(organizationId: organizationId, received: received),
            requestParameters,
            resolveDependencies
        );
    }

    public async Task<ReferralResponse> GetReferralApi(Guid organizationId, Guid id)
    {
        var referral =
            await GetReferralById(organizationId, id)
            ?? throw new NotFoundException("Referral not found.");
        var referralResponse = _mapper.Map<ReferralResponse>(referral);

        if (referral != null)
            await resolveDependencies(referralResponse);

        return referralResponse;
    }

    public async Task<Referral> GetReferralById(Guid organizationId, Guid id)
    {
        return await _context
            .Referrals.Where(e =>
                e.OrganizationCreatedId == organizationId
                || e.OrganizationReferredToId == organizationId
            )
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<ReferralCaseNumberResponse> GetReferralByCaseNumberApi(string caseNumber)
    {
        var refferal =
            await _context.Referrals.FirstOrDefaultAsync(e => e.CaseNumber == caseNumber)
            ?? throw new NotFoundException("Referral not found.");
        var referralResponse = _mapper.Map<ReferralCaseNumberResponse>(refferal);
        var organizationReffereTo = await _context.Organizations.FirstOrDefaultAsync(e =>
            e.Id == referralResponse.OrganizationReferredToId
        );
        var organizationCreated = await _context.Organizations.FirstOrDefaultAsync(e =>
            e.Id == referralResponse.OrganizationCreatedId
        );
        referralResponse.OrganizationReferredTo = _mapper.Map<OrganizationResponse>(
            organizationReffereTo
        );
        referralResponse.OrganizationCreated = _mapper.Map<OrganizationResponse>(
            organizationCreated
        );

        return referralResponse;
    }

    public async Task<Referral> AddReferral(Guid organizationId, ReferralAddRequest model)
    {
        if (!model.IsDraft)
        {
            var referredOrganization =
                await _context.Organizations.FirstOrDefaultAsync(e =>
                    e.Id == model.OrganizationReferredToId
                ) ?? throw new NotFoundException("Organization not found.");
        }

        var referral = _mapper.Map<Referral>(model);
        referral.OrganizationCreatedId = organizationId;
        referral.CaseNumber = GuidHelper.ToShortString(Guid.NewGuid());
        referral.Status = ReferralStatus.UnderReview;

        var newReferral = _context.Referrals.Add(referral).Entity;
        await _context.SaveChangesAsync();

        return newReferral;
    }

    public async Task<Referral> UpdateReferral(Referral referral)
    {
        var updatedReferral = _context.Referrals.Update(referral).Entity;
        await _context.SaveChangesAsync();

        return updatedReferral;
    }

    public async Task DeleteReferral(Referral referral)
    {
        if (referral.FileIds != null)
        {
            foreach (var fileId in referral.FileIds)
            {
                try
                {
                    var file = await _storageService.GetFileById(fileId);
                    await _storageService.DeleteFile(file);
                }
                catch (NotFoundException)
                {
                    // File already gone — continue cleanup of remaining files.
                }
            }
        }

        _context.Discussions.RemoveRange(
            _context.Discussions.Where(e => e.ReferralId == referral.Id)
        );
        _context.Referrals.Remove(referral);
        await _context.SaveChangesAsync();
    }

    public async Task<Discussion> AddDiscussion(Guid referralId, DiscussionAddRequest model)
    {
        var referral =
            await _context.Referrals.FirstOrDefaultAsync(e => e.Id == referralId)
            ?? throw new NotFoundException("Referral not found.");
        var discussion = _mapper.Map<Discussion>(model);
        discussion.ReferralId = referralId;

        var newDiscussion = _context.Discussions.Add(discussion).Entity;
        await _context.SaveChangesAsync();

        return newDiscussion;
    }

    public async Task<Discussion> AddDiscussionBot(Guid referralId, DiscussionAddRequest model)
    {
        var referral =
            await _context.Referrals.FirstOrDefaultAsync(e => e.Id == referralId)
            ?? throw new NotFoundException("Referral not found.");
        var discussion = _mapper.Map<Discussion>(model);
        discussion.ReferralId = referralId;
        discussion.IsBot = true;

        var newDiscussion = _context.Discussions.Add(discussion).Entity;
        await _context.SaveChangesAsync();

        return newDiscussion;
    }

    public async Task<DiscussionResponse> GetDiscussionApi(Guid id)
    {
        var discussion =
            await _context.Discussions.FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new NotFoundException("Discussion not found.");
        var discussionResponse = _mapper.Map<DiscussionResponse>(discussion);
        var userResponse = await _userService.GetUserApi(id: discussion.UserCreatedId);
        userResponse.Permissions = null;
        discussionResponse.UserCreated = userResponse;

        return discussionResponse;
    }

    public async Task<List<DiscussionResponse>> GetDiscussionsApi(Guid referralId)
    {
        var referral =
            await _context.Referrals.FirstOrDefaultAsync(e => e.Id == referralId)
            ?? throw new NotFoundException("Referral not found.");
        var discussions = await _context
            .Discussions.Where(e => e.ReferralId == referralId)
            .ToListAsync();
        var discussionsResponse = _mapper.Map<List<DiscussionResponse>>(discussions);
        for (var i = 0; i < discussionsResponse.Count; i++)
        {
            var userResponse = await _userService.GetUserApi(id: discussions[i].UserCreatedId);
            userResponse.Permissions = null;
            discussionsResponse[i].UserCreated = userResponse;
        }

        return discussionsResponse;
    }

    public async Task<string> GetUpdatedFieldText(ReferralPatchRequest model, Referral referral)
    {
        var updatedProperties = model.GetType().GetProperties();
        var oldProperties = referral.GetType().GetProperties();
        var updatedFields = updatedProperties.Where(x => x.GetValue(model) != null);
        var updatedFieldsText = "";

        foreach (var field in updatedFields)
        {
            var filedName = field.Name;
            var value = field.GetValue(model);
            var oldValue = oldProperties
                .FirstOrDefault(x => x.Name == field.Name)
                ?.GetValue(referral);

            if (field.Name == "OrganizationReferredToId")
            {
                var organization = await _context.Organizations.FirstOrDefaultAsync(e =>
                    e.Id == (Guid?)value
                );
                value = organization?.Name;
                var oldOrganization = await _context.Organizations.FirstOrDefaultAsync(e =>
                    e.Id == (Guid?)oldValue
                );
                oldValue = oldOrganization?.Name;
                filedName = "OrganizationReferredTo";
            }

            if (field.Name == "SubactivitiesIds")
            {
                var activities = await _context
                    .Activities.Where(e => ((List<Guid>)value).Contains(e.Id))
                    .ToListAsync();
                var activitiesTitles = activities.Select(e => e.Title);
                value = string.Join(", ", activitiesTitles);
                var oldActivities = await _context
                    .Activities.Where(e => ((List<Guid>)oldValue).Contains(e.Id))
                    .ToListAsync();
                var oldActivitiesTitles = oldActivities.Select(e => e.Title);
                oldValue = string.Join(", ", oldActivitiesTitles);
                filedName = "Subactivities";
            }

            if (field.Name == "FocalPointId")
            {
                var user = await _context.Users.FirstOrDefaultAsync(e => e.Id == (Guid)value);
                value = user.FirstName + " " + user.LastName;
                if (oldValue != null)
                {
                    var oldUser = await _context.Users.FirstOrDefaultAsync(e =>
                        e.Id == (Guid)oldValue
                    );
                    oldValue = oldUser.FirstName + " " + oldUser.LastName;
                }

                filedName = "FocalPoint";
            }

            updatedFieldsText += $"{filedName}:  {oldValue} => {value}<br>";
        }

        return updatedFieldsText;
    }

    public async Task<BatchCreateResponse> CreateBatchReferrals(
        Guid organizationId,
        Guid userId,
        BatchCreateRequest model
    )
    {
        var file = model.File ?? throw new BadRequestException("File is required");
        using var workbook = new XLWorkbook(file.OpenReadStream());

        var worksheet = workbook.Worksheet(1);
        var lastColumnIndex = worksheet.LastColumnUsed().ColumnNumber() + 1;
        var lastRowNumber = worksheet.LastRowUsed().RowNumber();

        var referralRecords = new List<ReferralAddRequest>();
        var missingRequiredFields = false;

        // receiving org specific info
        var organizationReferredToId = model.OrganizationReferredToId;
        var serviceCategory = model.ServiceCategory;
        var subactivitiesIds = model.SubactivitiesIds ?? [];

        var requiredGeneralBeneficiaryFields = new Dictionary<string, int>
        {
            { "FirstName", (int)GeneralReferralsWorksheetColumns.FirstName },
            { "Surname", (int)GeneralReferralsWorksheetColumns.Surname },
            { "PartronymicName", (int)GeneralReferralsWorksheetColumns.PatronymicName },
            { "Gender", (int)GeneralReferralsWorksheetColumns.Gender },
            { "Required", (int)GeneralReferralsWorksheetColumns.Required }
        };

        for (var i = 2; i <= lastRowNumber; i++)
        {
            var referralRecord = new ReferralAddRequest
            {
                OrganizationReferredToId = organizationReferredToId,
                ServiceCategory = serviceCategory,
                SubactivitiesIds = subactivitiesIds,
                IsBatchUploaded = true
            };
            // General beneficiary data
            var firstName = worksheet
                .Cell(i, (int)GeneralReferralsWorksheetColumns.FirstName)
                .Value.ToString();
            var surname = worksheet
                .Cell(i, (int)GeneralReferralsWorksheetColumns.Surname)
                .Value.ToString();
            var patronymicName = worksheet
                .Cell(i, (int)GeneralReferralsWorksheetColumns.PatronymicName)
                .Value.ToString();
            var dateOfBirth = BatchReferralsValidators.ValidateAndParseDateOfBirth(
                worksheet,
                i,
                (int)GeneralReferralsWorksheetColumns.DateOfBirth,
                ref missingRequiredFields
            );
            var gender = worksheet
                .Cell(i, (int)GeneralReferralsWorksheetColumns.Gender)
                .Value.ToString();
            var taxId = worksheet
                .Cell(i, (int)GeneralReferralsWorksheetColumns.TaxId)
                .Value.ToString();
            var address = worksheet
                .Cell(i, (int)GeneralReferralsWorksheetColumns.Address)
                .Value.ToString();

            var administrativeRegion1Id =
                await _administrativeRegionService.GetAdministrativeRegionByNameApi(
                    worksheet
                        .Cell(i, (int)GeneralReferralsWorksheetColumns.AdministrativeRegion1)
                        .Value.ToString(),
                    1
                );
            var administrativeRegion2Id =
                await _administrativeRegionService.GetAdministrativeRegionByNameApi(
                    worksheet
                        .Cell(i, (int)GeneralReferralsWorksheetColumns.AdministrativeRegion2)
                        .Value.ToString(),
                    2
                );
            var administrativeRegion3Id =
                await _administrativeRegionService.GetAdministrativeRegionByNameApi(
                    worksheet
                        .Cell(i, (int)GeneralReferralsWorksheetColumns.AdministrativeRegion3)
                        .Value.ToString(),
                    3
                );
            var administrativeRegion4Id =
                await _administrativeRegionService.GetAdministrativeRegionByNameApi(
                    worksheet
                        .Cell(i, (int)GeneralReferralsWorksheetColumns.AdministrativeRegion4)
                        .Value.ToString(),
                    4
                );
            ;

            var email = worksheet
                .Cell(i, (int)GeneralReferralsWorksheetColumns.Email)
                .Value.ToString();
            var phone = worksheet
                .Cell(i, (int)GeneralReferralsWorksheetColumns.Phone)
                .Value.ToString();
            var contactPreference = worksheet
                .Cell(i, (int)GeneralReferralsWorksheetColumns.ContactPreference)
                .Value.ToString();

            var restrictions = worksheet
                .Cell(i, (int)GeneralReferralsWorksheetColumns.Restrictions)
                .Value.ToString();
            var consent = worksheet
                .Cell(i, (int)GeneralReferralsWorksheetColumns.Consent)
                .Value.ToString()
                .ToLower() switch
            {
                "true" => true,
                _
                    => HighlightCellAndReturnNull(
                        worksheet.Cell(i, (int)GeneralReferralsWorksheetColumns.Consent),
                        ref missingRequiredFields
                    )
            };
            var required = worksheet
                .Cell(i, (int)GeneralReferralsWorksheetColumns.Required)
                .Value.ToString();
            var needForService = worksheet
                .Cell(i, (int)GeneralReferralsWorksheetColumns.NeedForService)
                .Value.ToString();

            referralRecord.FirstName = firstName;
            referralRecord.Surname = surname;
            referralRecord.PatronymicName = patronymicName;
            referralRecord.DateOfBirth = dateOfBirth;
            referralRecord.Gender = gender;
            referralRecord.TaxId = taxId;
            referralRecord.Address = address;
            referralRecord.AdministrativeRegion1Id = administrativeRegion1Id;
            referralRecord.AdministrativeRegion2Id = administrativeRegion2Id;
            referralRecord.AdministrativeRegion3Id = administrativeRegion3Id;
            referralRecord.AdministrativeRegion4Id = administrativeRegion4Id;
            referralRecord.Email = email;
            referralRecord.Phone = phone;
            referralRecord.ContactPreference = contactPreference;
            referralRecord.Restrictions = restrictions;
            referralRecord.Consent = consent;
            referralRecord.Required = required;
            referralRecord.NeedForService = needForService;

            // MPCA Service Cat specific fields
            if (model.BatchType == BatchType.BeneficiariesWithMpca)
            {
                var displacementStatus = worksheet
                    .Cell(i, (int)MpcaReferralsWorksheetColumns.DisplacementStatus)
                    .Value.ToString()
                    .ToLower();
                var householdSize = worksheet
                    .Cell(i, (int)MpcaReferralsWorksheetColumns.HouseholdSize)
                    .Value.ToString();
                var householdMonthlyIncome = worksheet
                    .Cell(i, (int)MpcaReferralsWorksheetColumns.HouseholdMonthlyIncome)
                    .Value.ToString();

                referralRecord.DisplacementStatus = displacementStatus;
                referralRecord.HouseholdSize = householdSize;
                referralRecord.HouseholdMonthlyIncome = householdMonthlyIncome;

                var vulnerabilityNames = new List<string>
                {
                    "householdWithPregnantPersons",
                    "householdOfElderly",
                    "householdAffectedByConflict",
                    "householdWithGroupDisability",
                    "householdWithSeriousHealthIssues",
                    "householdWith3OrMoreChildren",
                    "highlyVulnerableIDPHouseholds",
                    "householdsWithChildrenUpTo2Years",
                    "singleHeadedHouseholdsIncludingWomanHeaded",
                    "singleParentHouseholds"
                };

                referralRecord.HouseholdsVulnerabilityCriteria = new List<string>();

                for (
                    var columnIndex = (int)MpcaReferralsWorksheetColumns.VulnerabilityCriteria1;
                    columnIndex <= (int)MpcaReferralsWorksheetColumns.VulnerabilityCriteria10;
                    columnIndex++
                )
                {
                    var cellValue = worksheet.Cell(i, columnIndex).Value.ToString().ToLower();
                    var isTrue = cellValue == "true"; // Check if it's "true" (case-insensitive)

                    if (isTrue)
                    {
                        var nameIndex =
                            columnIndex - (int)MpcaReferralsWorksheetColumns.VulnerabilityCriteria1;
                        referralRecord.HouseholdsVulnerabilityCriteria.Add(
                            vulnerabilityNames[nameIndex]
                        );
                    }
                }
            }

            // Minor specific fields
            if (model.BatchType == BatchType.BeneficiaryMinors)
            {
                var isSeparated = worksheet
                    .Cell(i, (int)MinorReferralsWorksheetColumns.IsSeparated)
                    .Value.ToString()
                    .ToLower() switch
                {
                    "true" => true,
                    "false" => false,
                    _
                        => HighlightCellAndReturnNull(
                            worksheet.Cell(i, (int)MinorReferralsWorksheetColumns.IsSeparated),
                            ref missingRequiredFields
                        )
                };
                var caregiver = worksheet
                    .Cell(i, (int)MinorReferralsWorksheetColumns.Caregiver)
                    .Value.ToString();
                var relationshipToChild = worksheet
                    .Cell(i, (int)MinorReferralsWorksheetColumns.RelationshipToChild)
                    .Value.ToString();
                var caregiverEmail = worksheet
                    .Cell(i, (int)MinorReferralsWorksheetColumns.CaregiverEmail)
                    .Value.ToString();
                var caregiverPhone = worksheet
                    .Cell(i, (int)MinorReferralsWorksheetColumns.CaregiverPhone)
                    .Value.ToString();
                var caregiverContactPreference = worksheet
                    .Cell(i, (int)MinorReferralsWorksheetColumns.CaregiverContactPreference)
                    .Value.ToString();
                var isCaregiverInformed = worksheet
                    .Cell(i, (int)MinorReferralsWorksheetColumns.IsCaregiverInformed)
                    .Value.ToString()
                    .ToLower() switch
                {
                    "true" => true,
                    "false" => false,
                    _
                        => HighlightCellAndReturnNull(
                            worksheet.Cell(
                                i,
                                (int)MinorReferralsWorksheetColumns.IsCaregiverInformed
                            ),
                            ref missingRequiredFields
                        )
                };
                var caregiverExplanation = worksheet
                    .Cell(i, (int)MinorReferralsWorksheetColumns.CaregiverExplanation)
                    .Value.ToString();
                var caregiverNote = worksheet
                    .Cell(i, (int)MinorReferralsWorksheetColumns.CaregiverNote)
                    .Value.ToString();

                BatchReferralsValidators.ValidateCaregiverFields(
                    worksheet,
                    i,
                    isSeparated,
                    caregiver,
                    relationshipToChild,
                    caregiverEmail,
                    caregiverPhone,
                    caregiverContactPreference,
                    isCaregiverInformed,
                    caregiverExplanation,
                    caregiverNote,
                    ref missingRequiredFields
                );

                referralRecord.IsSeparated = isSeparated;
                referralRecord.Caregiver = caregiver;
                referralRecord.RelationshipToChild = relationshipToChild;
                referralRecord.CaregiverEmail = caregiverEmail;
                referralRecord.CaregiverPhone = caregiverPhone;
                referralRecord.CaregiverContactPreference = caregiverContactPreference;
                referralRecord.IsCaregiverInformed = isCaregiverInformed;
                referralRecord.CaregiverExplanation = caregiverExplanation;
                referralRecord.CaregiverNote = caregiverNote;
            }

            // basic required fields validation (empty check only)
            foreach (var field in requiredGeneralBeneficiaryFields)
                BatchReferralsValidators.ValidateAndHighlightRequiredField(
                    worksheet,
                    i,
                    field.Value,
                    worksheet.Cell(i, field.Value).Value.ToString(),
                    ref missingRequiredFields
                );

            // custom validations for general beneficiary data
            BatchReferralsValidators.ValidateContactPreference(
                worksheet,
                i,
                contactPreference,
                email,
                phone,
                ref missingRequiredFields
            );

            referralRecords.Add(referralRecord);
        }

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);

        var savedFile = await _storageService.SaveFile(
            StorageType.GetById(StorageType.Assets.Id),
            memoryStream,
            userId,
            model.File.FileName
        );
        var fileResponse = await _storageService.GetFileApiById(savedFile.Id);

        if (missingRequiredFields)
            return new BatchCreateResponse { MissingRequiredFields = true, File = fileResponse };

        foreach (var rf in referralRecords)
            await AddReferral(organizationId, rf);

        return new BatchCreateResponse { MissingRequiredFields = false, File = fileResponse };
    }

    private bool HighlightCellAndReturnNull(IXLCell cell, ref bool missingRequiredFields)
    {
        cell.Style.Fill.BackgroundColor = XLColor.Red;
        missingRequiredFields = true;
        return false;
    }

    private async Task resolveDependencies(ReferralResponse referral)
    {
        if (!referral.IsDraft)
            referral.OrganizationReferredTo = await _organizationService.GetOrganizationApi(
                referral.OrganizationReferredToId
            );

        var organizationCreated = await _context.Organizations.FirstOrDefaultAsync(e =>
            e.Id == referral.OrganizationCreatedId
        );
        var userCreated = await _context.Users.FirstOrDefaultAsync(e =>
            e.Id == referral.UserCreatedId
        );
        referral.OrganizationCreated = _mapper.Map<OrganizationResponse>(organizationCreated);
        referral.UserCreated = _mapper.Map<UserResponse>(userCreated);

        if (referral.SubactivitiesIds != null && referral.SubactivitiesIds.Count > 0)
        {
            var activities = await _context
                .Activities.Where(e => referral.SubactivitiesIds.Contains(e.Id))
                .ToListAsync();
            referral.Subactivities = _mapper.Map<List<Activity>>(activities);
        }

        if (referral.FocalPointId != null)
        {
            var userFocalPoint = await _context.Users.FirstOrDefaultAsync(e =>
                e.Id == referral.FocalPointId
            );
            referral.FocalPoint = _mapper.Map<UserResponse>(userFocalPoint);
        }

        if (referral.FileIds != null && referral.FileIds.Count > 0)
            referral.Files = await _storageService.GetFilesApiById(referral.FileIds);

        if (referral.AdministrativeRegion1Id != null)
            referral.AdministrativeRegion1 =
                await _administrativeRegionService.GetAdministrativeRegionApi(
                    referral.AdministrativeRegion1Id.Value
                );

        if (referral.AdministrativeRegion2Id != null)
            referral.AdministrativeRegion2 =
                await _administrativeRegionService.GetAdministrativeRegionApi(
                    referral.AdministrativeRegion2Id.Value
                );

        if (referral.AdministrativeRegion3Id != null)
            referral.AdministrativeRegion3 =
                await _administrativeRegionService.GetAdministrativeRegionApi(
                    referral.AdministrativeRegion3Id.Value
                );

        if (referral.AdministrativeRegion4Id != null)
            referral.AdministrativeRegion4 =
                await _administrativeRegionService.GetAdministrativeRegionApi(
                    referral.AdministrativeRegion4Id.Value
                );
    }
}
