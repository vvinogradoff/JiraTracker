using System.Text.Json.Serialization;

namespace UpworkJiraTracker.Model;

#region GET /deelapi/time_tracking/profiles/contracts

public class DeelContractsResponse
{
    [JsonPropertyName("contracts")]
    public List<DeelContractSummary> Contracts { get; set; } = new();
}

public class DeelContractSummary
{
    [JsonPropertyName("contractId")]
    public string ContractId { get; set; } = "";

    [JsonPropertyName("organizationId")]
    public int OrganizationId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("jobTitleName")]
    public string? JobTitleName { get; set; }

    [JsonPropertyName("contractType")]
    public string? ContractType { get; set; }

    [JsonPropertyName("canSubmitShifts")]
    public bool CanSubmitShifts { get; set; }

    [JsonPropertyName("isTimeTrackingEnabled")]
    public bool IsTimeTrackingEnabled { get; set; }

    [JsonPropertyName("timeTrackingPolicy")]
    public DeelTimeTrackingPolicy? TimeTrackingPolicy { get; set; }

    [JsonPropertyName("employeeContractId")]
    public string? EmployeeContractId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("isHourly")]
    public bool IsHourly { get; set; }

    [JsonPropertyName("canSubmitHours")]
    public bool CanSubmitHours { get; set; }
}

public class DeelTimeTrackingPolicy
{
    [JsonPropertyName("timeSubmissionMethods")]
    public List<string> TimeSubmissionMethods { get; set; } = new();

    [JsonPropertyName("canEditTimeClockShifts")]
    public bool CanEditTimeClockShifts { get; set; }

    [JsonPropertyName("isDayTracking")]
    public bool IsDayTracking { get; set; }

    [JsonPropertyName("workLocationEnablement")]
    public string? WorkLocationEnablement { get; set; }

    [JsonPropertyName("allowRemoteAsWorkLocation")]
    public bool AllowRemoteAsWorkLocation { get; set; }

    [JsonPropertyName("geofencingEnablement")]
    public string? GeofencingEnablement { get; set; }

    [JsonPropertyName("workLocations")]
    public List<object> WorkLocations { get; set; } = new();

    [JsonPropertyName("isWorkDescriptionMandatory")]
    public bool IsWorkDescriptionMandatory { get; set; }
}

#endregion

#region POST /deelapi/time_tracking/time_sheets/shifts

public class DeelLogShiftRequest
{
    [JsonPropertyName("contractOid")]
    public string ContractOid { get; set; } = "";

    [JsonPropertyName("start")]
    public string Start { get; set; } = "";

    [JsonPropertyName("totalWorkedHours")]
    public double TotalWorkedHours { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "BULK";

    [JsonPropertyName("submitType")]
    public string SubmitType { get; set; } = "BULK";

    [JsonPropertyName("hourlyReportPresetId")]
    public string HourlyReportPresetId { get; set; } = "default";

    [JsonPropertyName("isAutoApproved")]
    public bool IsAutoApproved { get; set; } = false;

    [JsonPropertyName("origin")]
    public string Origin { get; set; } = "PLATFORM";

    [JsonPropertyName("workLocation")]
    public object? WorkLocation { get; set; } = null;

    [JsonPropertyName("workLocationEntityAddressId")]
    public object? WorkLocationEntityAddressId { get; set; } = null;

    [JsonPropertyName("shiftType")]
    public string ShiftType { get; set; } = "UNSPECIFIED";

    [JsonPropertyName("isForecastEdit")]
    public bool IsForecastEdit { get; set; } = false;
}

public class DeelLogShiftResponse
{
    [JsonPropertyName("shift")]
    public DeelShift? Shift { get; set; }
}

public class DeelShift
{
    [JsonPropertyName("publicId")]
    public string? PublicId { get; set; }

    [JsonPropertyName("organizationId")]
    public int OrganizationId { get; set; }

    [JsonPropertyName("createdBy")]
    public int CreatedBy { get; set; }

    [JsonPropertyName("contractId")]
    public string? ContractId { get; set; }

    [JsonPropertyName("totalHours")]
    public string? TotalHours { get; set; }

    [JsonPropertyName("totalBreak")]
    public string? TotalBreak { get; set; }

    [JsonPropertyName("totalWorkedHours")]
    public string? TotalWorkedHours { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("editReason")]
    public string? EditReason { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("day")]
    public string? Day { get; set; }

    [JsonPropertyName("start")]
    public string? Start { get; set; }

    [JsonPropertyName("end")]
    public string? End { get; set; }

    [JsonPropertyName("shiftType")]
    public string? ShiftType { get; set; }

    [JsonPropertyName("createdAt")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public string? UpdatedAt { get; set; }

    [JsonPropertyName("calculatedPayment")]
    public string? CalculatedPayment { get; set; }

    [JsonPropertyName("hourlyReportId")]
    public string? HourlyReportId { get; set; }

    [JsonPropertyName("submitType")]
    public string? SubmitType { get; set; }

    [JsonPropertyName("approvalRequestId")]
    public string? ApprovalRequestId { get; set; }

    [JsonPropertyName("isHalfDay")]
    public bool IsHalfDay { get; set; }

    [JsonPropertyName("mealBreakWaived")]
    public bool MealBreakWaived { get; set; }

    [JsonPropertyName("workAssignmentId")]
    public string? WorkAssignmentId { get; set; }

    [JsonPropertyName("workLocation")]
    public object? WorkLocation { get; set; }

    [JsonPropertyName("workLocationEntityAddressId")]
    public object? WorkLocationEntityAddressId { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

#endregion

#region GET /deelapi/contracts/{contractId}

public class DeelContractDetailsResponse
{
    [JsonPropertyName("wasCreatedByProfileParty")]
    public bool WasCreatedByProfileParty { get; set; }

    [JsonPropertyName("canBeCancelled")]
    public bool CanBeCancelled { get; set; }

    [JsonPropertyName("canBeRejected")]
    public bool CanBeRejected { get; set; }

    [JsonPropertyName("isArchived")]
    public bool IsArchived { get; set; }

    [JsonPropertyName("isDeleted")]
    public bool IsDeleted { get; set; }

    [JsonPropertyName("hrisIsActive")]
    public bool HrisIsActive { get; set; }

    [JsonPropertyName("inactivityInDays")]
    public int InactivityInDays { get; set; }

    [JsonPropertyName("timeTracking")]
    public DeelTimeTrackingInfo? TimeTracking { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("oid")]
    public string? Oid { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("specialClause")]
    public string? SpecialClause { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("contractType")]
    public string? ContractType { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("createdAt")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("cancelledAt")]
    public string? CancelledAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public string? UpdatedAt { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("timezone")]
    public string? Timezone { get; set; }

    [JsonPropertyName("effectiveDate")]
    public string? EffectiveDate { get; set; }

    [JsonPropertyName("effectivePlainDate")]
    public string? EffectivePlainDate { get; set; }

    [JsonPropertyName("initialEffectivePlainDate")]
    public string? InitialEffectivePlainDate { get; set; }

    [JsonPropertyName("completedAt")]
    public string? CompletedAt { get; set; }

    [JsonPropertyName("completionDate")]
    public string? CompletionDate { get; set; }

    [JsonPropertyName("completionPlainDate")]
    public string? CompletionPlainDate { get; set; }

    [JsonPropertyName("terminationNoticeDays")]
    public int TerminationNoticeDays { get; set; }

    [JsonPropertyName("firstSign")]
    public string? FirstSign { get; set; }

    [JsonPropertyName("clientSignedAt")]
    public string? ClientSignedAt { get; set; }

    [JsonPropertyName("contractorSignedAt")]
    public string? ContractorSignedAt { get; set; }

    [JsonPropertyName("clientSignature")]
    public string? ClientSignature { get; set; }

    [JsonPropertyName("contractorSignature")]
    public string? ContractorSignature { get; set; }

    [JsonPropertyName("invitedContractorEmail")]
    public string? InvitedContractorEmail { get; set; }

    [JsonPropertyName("expectedContractorEmail")]
    public string? ExpectedContractorEmail { get; set; }

    [JsonPropertyName("invitedClientEmail")]
    public string? InvitedClientEmail { get; set; }

    [JsonPropertyName("contractorExpectedFirstName")]
    public string? ContractorExpectedFirstName { get; set; }

    [JsonPropertyName("contractorExpectedLastName")]
    public string? ContractorExpectedLastName { get; set; }

    [JsonPropertyName("signedCompletionDate")]
    public string? SignedCompletionDate { get; set; }

    [JsonPropertyName("signedDate")]
    public string? SignedDate { get; set; }

    [JsonPropertyName("isAttachmentSigned")]
    public bool IsAttachmentSigned { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("isMainIncome")]
    public bool IsMainIncome { get; set; }

    [JsonPropertyName("withholdingTaxPercentage")]
    public double? WithholdingTaxPercentage { get; set; }

    [JsonPropertyName("isPaidOutsideOfDeel")]
    public bool IsPaidOutsideOfDeel { get; set; }

    [JsonPropertyName("workPermitType")]
    public string? WorkPermitType { get; set; }

    [JsonPropertyName("seniority")]
    public string? Seniority { get; set; }

    [JsonPropertyName("jobTitle")]
    public DeelJobTitle? JobTitle { get; set; }

    [JsonPropertyName("jobTitleName")]
    public string? JobTitleName { get; set; }

    [JsonPropertyName("teamId")]
    public int TeamId { get; set; }

    [JsonPropertyName("documentTemplateId")]
    public string? DocumentTemplateId { get; set; }

    [JsonPropertyName("client")]
    public DeelPerson? Client { get; set; }

    [JsonPropertyName("clientLegalEntity")]
    public DeelLegalEntity? ClientLegalEntity { get; set; }

    [JsonPropertyName("contractor")]
    public DeelPerson? Contractor { get; set; }

    [JsonPropertyName("contractorLegalEntity")]
    public DeelContractorLegalEntity? ContractorLegalEntity { get; set; }

    [JsonPropertyName("creator")]
    public DeelCreator? Creator { get; set; }

    [JsonPropertyName("contractorHrisProfile")]
    public DeelHrisProfile? ContractorHrisProfile { get; set; }

    [JsonPropertyName("isCreator")]
    public bool IsCreator { get; set; }

    [JsonPropertyName("isClient")]
    public bool IsClient { get; set; }

    [JsonPropertyName("isContractor")]
    public bool IsContractor { get; set; }

    [JsonPropertyName("isCreatorContractor")]
    public bool IsCreatorContractor { get; set; }

    [JsonPropertyName("data")]
    public DeelContractData? Data { get; set; }

    [JsonPropertyName("isEorContract")]
    public bool IsEorContract { get; set; }

    [JsonPropertyName("isPEOContract")]
    public bool IsPEOContract { get; set; }

    [JsonPropertyName("needsReportApproval")]
    public bool NeedsReportApproval { get; set; }

    [JsonPropertyName("canSkipDeposit")]
    public bool CanSkipDeposit { get; set; }

    [JsonPropertyName("recommendedManagementFeeUSD")]
    public string? RecommendedManagementFeeUSD { get; set; }

    [JsonPropertyName("team")]
    public DeelTeam? Team { get; set; }

    [JsonPropertyName("workStatements")]
    public List<DeelWorkStatement> WorkStatements { get; set; } = new();

    [JsonPropertyName("customFields")]
    public List<object> CustomFields { get; set; } = new();

    [JsonPropertyName("taxForm")]
    public DeelTaxForm? TaxForm { get; set; }

    [JsonPropertyName("ContractIntegrations")]
    public List<object> ContractIntegrations { get; set; } = new();

    [JsonPropertyName("commissions")]
    public List<object> Commissions { get; set; } = new();

    [JsonPropertyName("total")]
    public DeelTotal? Total { get; set; }

    [JsonPropertyName("availableFinalPaymentStartDate")]
    public string? AvailableFinalPaymentStartDate { get; set; }

    [JsonPropertyName("paymentCycles")]
    public List<DeelPaymentCycle> PaymentCycles { get; set; } = new();

    [JsonPropertyName("overdueCount")]
    public int OverdueCount { get; set; }

    [JsonPropertyName("previousCycleDates")]
    public DeelCycleDates? PreviousCycleDates { get; set; }

    [JsonPropertyName("completedPaymentCycles")]
    public int CompletedPaymentCycles { get; set; }

    [JsonPropertyName("offCycles")]
    public List<object> OffCycles { get; set; } = new();

    [JsonPropertyName("milestones")]
    public List<object> Milestones { get; set; } = new();

    [JsonPropertyName("timeOffs")]
    public List<object> TimeOffs { get; set; } = new();

    [JsonPropertyName("timeOffReports")]
    public List<object> TimeOffReports { get; set; } = new();

    [JsonPropertyName("actualContractTermination")]
    public object? ActualContractTermination { get; set; }

    [JsonPropertyName("signFlowType")]
    public string? SignFlowType { get; set; }

    [JsonPropertyName("clientSignActionType")]
    public string? ClientSignActionType { get; set; }

    [JsonPropertyName("contractorSignActionType")]
    public string? ContractorSignActionType { get; set; }

    [JsonPropertyName("documentFilename")]
    public string? DocumentFilename { get; set; }

    [JsonPropertyName("documentUrl")]
    public string? DocumentUrl { get; set; }

    [JsonPropertyName("canSubmitExpenses")]
    public bool CanSubmitExpenses { get; set; }

    [JsonPropertyName("canBeReinstated")]
    public bool CanBeReinstated { get; set; }

    [JsonPropertyName("employmentCosts")]
    public object? EmploymentCosts { get; set; }

    [JsonPropertyName("hrisInfo")]
    public DeelHrisInfo? HrisInfo { get; set; }

    [JsonPropertyName("mobility")]
    public object? Mobility { get; set; }

    [JsonPropertyName("backgroundCheckData")]
    public object? BackgroundCheckData { get; set; }

    [JsonPropertyName("variableCompensations")]
    public List<object> VariableCompensations { get; set; } = new();
}

public class DeelTimeTrackingInfo
{
    [JsonPropertyName("isHourly")]
    public bool IsHourly { get; set; }

    [JsonPropertyName("hourlyRate")]
    public string? HourlyRate { get; set; }

    [JsonPropertyName("schedule")]
    public object? Schedule { get; set; }

    [JsonPropertyName("ratePolicy")]
    public object? RatePolicy { get; set; }

    [JsonPropertyName("submissionMethod")]
    public object? SubmissionMethod { get; set; }

    [JsonPropertyName("gpSettings")]
    public DeelGpSettings? GpSettings { get; set; }

    [JsonPropertyName("peoSettings")]
    public DeelGpSettings? PeoSettings { get; set; }

    [JsonPropertyName("hrisSettings")]
    public DeelGpSettings? HrisSettings { get; set; }

    [JsonPropertyName("flsaOvertimeStatus")]
    public string? FlsaOvertimeStatus { get; set; }

    [JsonPropertyName("canSubmitHours")]
    public bool CanSubmitHours { get; set; }

    [JsonPropertyName("overtimeAcknowledgement")]
    public string? OvertimeAcknowledgement { get; set; }
}

public class DeelGpSettings
{
    [JsonPropertyName("submittingHours")]
    public string? SubmittingHours { get; set; }
}

public class DeelJobTitle
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class DeelPerson
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("company")]
    public object? Company { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("firstname")]
    public string? Firstname { get; set; }

    [JsonPropertyName("jobTitle")]
    public string? JobTitle { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("picUrl")]
    public string? PicUrl { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("timezone")]
    public string? Timezone { get; set; }
}

public class DeelLegalEntity
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("legalType")]
    public string? LegalType { get; set; }

    [JsonPropertyName("entityType")]
    public string? EntityType { get; set; }

    [JsonPropertyName("registrationNumber")]
    public string? RegistrationNumber { get; set; }

    [JsonPropertyName("vatId")]
    public string? VatId { get; set; }

    [JsonPropertyName("useAddressAsPostalAddress")]
    public bool UseAddressAsPostalAddress { get; set; }

    [JsonPropertyName("address")]
    public DeelAddress? Address { get; set; }

    [JsonPropertyName("postalAddress")]
    public DeelAddress? PostalAddress { get; set; }

    [JsonPropertyName("registrationAddress")]
    public DeelRegistrationAddress? RegistrationAddress { get; set; }

    [JsonPropertyName("isDeelShield")]
    public bool IsDeelShield { get; set; }

    [JsonPropertyName("isEorLocalPaymentsEnabled")]
    public bool IsEorLocalPaymentsEnabled { get; set; }

    [JsonPropertyName("eorLocalPaymentsPercentage")]
    public double? EorLocalPaymentsPercentage { get; set; }

    [JsonPropertyName("onboardingCompletedAt")]
    public string? OnboardingCompletedAt { get; set; }

    [JsonPropertyName("registrationStatus")]
    public string? RegistrationStatus { get; set; }

    [JsonPropertyName("isCorLocalPaymentsEnabled")]
    public bool IsCorLocalPaymentsEnabled { get; set; }

    [JsonPropertyName("corLocalPaymentsPercentage")]
    public double? CorLocalPaymentsPercentage { get; set; }

    [JsonPropertyName("organization")]
    public DeelOrganization? Organization { get; set; }
}

public class DeelContractorLegalEntity
{
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("entityType")]
    public string? EntityType { get; set; }

    [JsonPropertyName("legalStatus")]
    public string? LegalStatus { get; set; }

    [JsonPropertyName("registrationNumber")]
    public string? RegistrationNumber { get; set; }

    [JsonPropertyName("citizenship")]
    public string? Citizenship { get; set; }

    [JsonPropertyName("taxResidence")]
    public string? TaxResidence { get; set; }

    [JsonPropertyName("legalType")]
    public string? LegalType { get; set; }

    [JsonPropertyName("vatCountry")]
    public string? VatCountry { get; set; }

    [JsonPropertyName("address")]
    public DeelSimpleAddress? Address { get; set; }

    [JsonPropertyName("registrationAddress")]
    public object? RegistrationAddress { get; set; }

    [JsonPropertyName("postalAddress")]
    public DeelSimpleAddress? PostalAddress { get; set; }

    [JsonPropertyName("useAddressAsPostalAddress")]
    public bool UseAddressAsPostalAddress { get; set; }
}

public class DeelAddress
{
    [JsonPropertyName("zip")]
    public string? Zip { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("street")]
    public string? Street { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("province")]
    public string? Province { get; set; }

    [JsonPropertyName("validatedAt")]
    public string? ValidatedAt { get; set; }

    [JsonPropertyName("validationResult")]
    public object? ValidationResult { get; set; }

    [JsonPropertyName("validationStatus")]
    public string? ValidationStatus { get; set; }
}

public class DeelSimpleAddress
{
    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("province")]
    public string? Province { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("street")]
    public string? Street { get; set; }

    [JsonPropertyName("zip")]
    public string? Zip { get; set; }
}

public class DeelRegistrationAddress
{
    [JsonPropertyName("state")]
    public string? State { get; set; }
}

public class DeelOrganization
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("logoUrl")]
    public string? LogoUrl { get; set; }
}

public class DeelCreator
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("firstname")]
    public string? Firstname { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }
}

public class DeelHrisProfile
{
    [JsonPropertyName("oid")]
    public string? Oid { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }
}

public class DeelContractData
{
    [JsonPropertyName("docsAreMandatory")]
    public bool DocsAreMandatory { get; set; }

    [JsonPropertyName("requestOptionalDocuments")]
    public bool RequestOptionalDocuments { get; set; }

    [JsonPropertyName("isMainIncome")]
    public bool IsMainIncome { get; set; }
}

public class DeelTeam
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("company")]
    public object? Company { get; set; }

    [JsonPropertyName("invoiceReviewDays")]
    public int InvoiceReviewDays { get; set; }

    [JsonPropertyName("massApprove")]
    public string? MassApprove { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("picUrl")]
    public string? PicUrl { get; set; }

    [JsonPropertyName("Organization")]
    public DeelTeamOrganization? Organization { get; set; }
}

public class DeelTeamOrganization
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("canSkipDeposit")]
    public bool CanSkipDeposit { get; set; }

    [JsonPropertyName("managementFeeUSD")]
    public string? ManagementFeeUSD { get; set; }

    [JsonPropertyName("icOrganizationSettings")]
    public object? IcOrganizationSettings { get; set; }
}

public class DeelWorkStatement
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("rate")]
    public double Rate { get; set; }

    [JsonPropertyName("firstPayment")]
    public double FirstPayment { get; set; }

    [JsonPropertyName("scale")]
    public string? Scale { get; set; }

    [JsonPropertyName("customName")]
    public string? CustomName { get; set; }

    [JsonPropertyName("effectiveDate")]
    public string? EffectiveDate { get; set; }

    [JsonPropertyName("effectivePlainDate")]
    public string? EffectivePlainDate { get; set; }

    [JsonPropertyName("completionDate")]
    public string? CompletionDate { get; set; }

    [JsonPropertyName("completionPlainDate")]
    public string? CompletionPlainDate { get; set; }

    [JsonPropertyName("terminationNoticeDays")]
    public int TerminationNoticeDays { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("specialClause")]
    public string? SpecialClause { get; set; }

    [JsonPropertyName("cycleScale")]
    public string? CycleScale { get; set; }

    [JsonPropertyName("cycleEnd")]
    public int CycleEnd { get; set; }

    [JsonPropertyName("cycleEndType")]
    public string? CycleEndType { get; set; }

    [JsonPropertyName("paymentDueType")]
    public string? PaymentDueType { get; set; }

    [JsonPropertyName("paymentDueDays")]
    public int PaymentDueDays { get; set; }

    [JsonPropertyName("payBeforeWeekends")]
    public bool PayBeforeWeekends { get; set; }

    [JsonPropertyName("firstPayDate")]
    public string? FirstPayDate { get; set; }

    [JsonPropertyName("firstPayPlainDate")]
    public string? FirstPayPlainDate { get; set; }

    [JsonPropertyName("clientSignedAt")]
    public string? ClientSignedAt { get; set; }

    [JsonPropertyName("contractorSignedAt")]
    public string? ContractorSignedAt { get; set; }

    [JsonPropertyName("signedAt")]
    public string? SignedAt { get; set; }

    [JsonPropertyName("createdAt")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public string? UpdatedAt { get; set; }

    [JsonPropertyName("jobTitleName")]
    public string? JobTitleName { get; set; }

    [JsonPropertyName("jobTitle")]
    public DeelJobTitle? JobTitle { get; set; }
}

public class DeelTaxForm
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }
}

public class DeelTotal
{
    [JsonPropertyName("amount")]
    public string? Amount { get; set; }

    [JsonPropertyName("reports")]
    public DeelTotalReports? Reports { get; set; }

    [JsonPropertyName("active")]
    public DeelTotalAmount? Active { get; set; }

    [JsonPropertyName("overdue")]
    public DeelTotalAmount? Overdue { get; set; }

    [JsonPropertyName("awaitingPayment")]
    public DeelTotalAmount? AwaitingPayment { get; set; }

    [JsonPropertyName("upcoming")]
    public DeelTotalAmount? Upcoming { get; set; }
}

public class DeelTotalReports
{
    [JsonPropertyName("pending")]
    public string? Pending { get; set; }
}

public class DeelTotalAmount
{
    [JsonPropertyName("amount")]
    public string? Amount { get; set; }

    [JsonPropertyName("bonus")]
    public string? Bonus { get; set; }
}

public class DeelPaymentCycle
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("start")]
    public string? Start { get; set; }

    [JsonPropertyName("end")]
    public string? End { get; set; }

    [JsonPropertyName("due")]
    public string? Due { get; set; }

    [JsonPropertyName("invoiceIssueDate")]
    public string? InvoiceIssueDate { get; set; }

    [JsonPropertyName("isWithholdingTaxEnabled")]
    public bool IsWithholdingTaxEnabled { get; set; }

    [JsonPropertyName("invoices")]
    public List<object> Invoices { get; set; } = new();

    [JsonPropertyName("msaInvoices")]
    public List<object> MsaInvoices { get; set; } = new();

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("isProcessing")]
    public bool IsProcessing { get; set; }

    [JsonPropertyName("fees")]
    public List<object> Fees { get; set; } = new();

    [JsonPropertyName("reports")]
    public List<DeelReport> Reports { get; set; } = new();

    [JsonPropertyName("total")]
    public DeelCycleTotal? Total { get; set; }
}

public class DeelReport
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("amount")]
    public double Amount { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("bonus")]
    public string? Bonus { get; set; }

    [JsonPropertyName("when")]
    public string? When { get; set; }

    [JsonPropertyName("createdAt")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("filename")]
    public string? Filename { get; set; }

    [JsonPropertyName("reviewReason")]
    public string? ReviewReason { get; set; }

    [JsonPropertyName("reviewedAt")]
    public string? ReviewedAt { get; set; }

    [JsonPropertyName("tax")]
    public string? Tax { get; set; }

    [JsonPropertyName("total")]
    public string? Total { get; set; }

    [JsonPropertyName("offCycleId")]
    public string? OffCycleId { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("sourceCurrency")]
    public string? SourceCurrency { get; set; }

    [JsonPropertyName("sourceTotal")]
    public string? SourceTotal { get; set; }

    [JsonPropertyName("exchangeRate")]
    public double? ExchangeRate { get; set; }

    [JsonPropertyName("rate")]
    public double Rate { get; set; }

    [JsonPropertyName("scale")]
    public string? Scale { get; set; }

    [JsonPropertyName("customScaleName")]
    public string? CustomScaleName { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("reviewer")]
    public object? Reviewer { get; set; }

    [JsonPropertyName("submittedType")]
    public string? SubmittedType { get; set; }

    [JsonPropertyName("reporter")]
    public DeelReporter? Reporter { get; set; }

    [JsonPropertyName("invoiceId")]
    public string? InvoiceId { get; set; }

    [JsonPropertyName("timeOffs")]
    public List<object> TimeOffs { get; set; } = new();

    [JsonPropertyName("percentage")]
    public double Percentage { get; set; }

    [JsonPropertyName("isRecurring")]
    public bool IsRecurring { get; set; }

    [JsonPropertyName("isSealed")]
    public bool IsSealed { get; set; }

    [JsonPropertyName("hourlyReportPreset")]
    public object? HourlyReportPreset { get; set; }

    [JsonPropertyName("approvalRequest")]
    public DeelApprovalRequest? ApprovalRequest { get; set; }

    [JsonPropertyName("details")]
    public DeelReportDetails? Details { get; set; }
}

public class DeelReporter
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("profileType")]
    public string? ProfileType { get; set; }

    [JsonPropertyName("profilePublicId")]
    public string? ProfilePublicId { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("picUrl")]
    public string? PicUrl { get; set; }

    [JsonPropertyName("employee")]
    public bool Employee { get; set; }
}

public class DeelApprovalRequest
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

public class DeelReportDetails
{
    [JsonPropertyName("isAutoApproved")]
    public bool IsAutoApproved { get; set; }

    [JsonPropertyName("sourceType")]
    public string? SourceType { get; set; }
}

public class DeelCycleTotal
{
    [JsonPropertyName("amount")]
    public string? Amount { get; set; }

    [JsonPropertyName("bonus")]
    public string? Bonus { get; set; }

    [JsonPropertyName("processing")]
    public string? Processing { get; set; }

    [JsonPropertyName("fees")]
    public string? Fees { get; set; }

    [JsonPropertyName("total")]
    public string? Total { get; set; }
}

public class DeelCycleDates
{
    [JsonPropertyName("reference")]
    public string? Reference { get; set; }

    [JsonPropertyName("originalStartPlainDate")]
    public string? OriginalStartPlainDate { get; set; }

    [JsonPropertyName("originalStart")]
    public string? OriginalStart { get; set; }

    [JsonPropertyName("startPlainDate")]
    public string? StartPlainDate { get; set; }

    [JsonPropertyName("start")]
    public string? Start { get; set; }

    [JsonPropertyName("endPlainDate")]
    public string? EndPlainDate { get; set; }

    [JsonPropertyName("end")]
    public string? End { get; set; }

    [JsonPropertyName("originalDuePlainDate")]
    public string? OriginalDuePlainDate { get; set; }

    [JsonPropertyName("originalDue")]
    public string? OriginalDue { get; set; }

    [JsonPropertyName("duePlainDate")]
    public string? DuePlainDate { get; set; }

    [JsonPropertyName("due")]
    public string? Due { get; set; }

    [JsonPropertyName("invoiceIssuePlainDate")]
    public string? InvoiceIssuePlainDate { get; set; }

    [JsonPropertyName("invoiceIssueDate")]
    public string? InvoiceIssueDate { get; set; }
}

public class DeelHrisInfo
{
    [JsonPropertyName("workEmail")]
    public string? WorkEmail { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("hrisProfileOid")]
    public string? HrisProfileOid { get; set; }

    [JsonPropertyName("orgStructures")]
    public List<object> OrgStructures { get; set; } = new();
}

#endregion
