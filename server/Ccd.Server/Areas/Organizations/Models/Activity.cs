using System;
using System.Text.Json.Serialization;
using Ccd.Server.Helpers;

namespace Ccd.Server.Organizations
{
    public class Activity
    {
        public Guid Id { get; set; } = IdProvider.NewId();
        public string Title { get; set; }
        public string ServiceType { get; set; }
        [JsonIgnore] public Guid OrganizationId { get; set; }
        [JsonIgnore] public Organization Organization { get; set; }
    }
}

public class ServiceType
{
    public const string Mpca = "mpca";
    public const string Wash = "wash";
    public const string Shelter = "shelter";
    public const string FoodAssistance = "foodAssistance";
    public const string Livelihoods = "livelihoods";
    public const string Protection = "protection";
    public const string IncentivePayments = "incentivePayments";

    public static bool IsValid(string serviceType)
    {
        return serviceType == Mpca || serviceType == Wash || serviceType == Shelter || serviceType == FoodAssistance || serviceType == Livelihoods || serviceType == Protection || serviceType == IncentivePayments;
    }
}