using System;
using System.Collections.Generic;
using System.Linq;

namespace Ccd.Server.Deduplication;

public static class ExcelFieldValidator
{
    // Allowed values (can be extended)
    private static readonly HashSet<string> AllowedOrganizations =
    [
        "UNICEF"
    ];

    private static readonly HashSet<string> AllowedActivities =
    [
        "MPCA"
    ];

    private static readonly HashSet<string> AllowedCurrencies =
    [
        "AED","AFN","ALL","AMD","ANG","AOA","ARS","AUD","AWG","AZN",
        "BAM","BBD","BDT","BGN","BHD","BIF","BMD","BND","BOB","BOV","BRL","BSD","BTN","BWP","BYN","BZD",
        "CAD","CDF","CHE","CHF","CHW","CLF","CLP","CNY","COP","COU","CRC","CUC","CUP","CVE","CZK",
        "DJF","DKK","DOP","DZD", "EGP","ERN","ETB","EUR", "FJD","FKP", "GBP","GEL","GHS","GIP","GMD","GNF","GTQ","GYD",
        "HKD","HNL","HTG","HUF", "IDR","ILS","INR","IQD","IRR","ISK", "JMD","JOD","JPY",
        "KES","KGS","KHR","KMF","KPW","KRW","KWD","KYD","KZT", "LAK","LBP","LKR","LRD","LSL","LYD",
        "MAD","MDL","MGA","MKD","MMK","MNT","MOP","MRU","MUR","MVR","MWK","MXN","MXV","MYR","MZN", "NAD","NGN","NIO","NOK","NPR","NZD",
        "OMR", "PAB","PEN","PGK","PHP","PKR","PLN","PYG", "QAR", "RON","RSD","CNY","RUB","RWF",
        "SAR","SBD","SCR","SDG","SEK","SGD","SHP","SLE","SLL","SOS","SRD","SSP","STN","SVC","SYP","SZL",
        "THB","TJS","TMT","TND","TOP","TRY","TTD","TWD","TZS", "UAH","UGX","USD","USN","UYI","UYU","UYW","UZS",
        "VED","VES","VND","VUV", "WST", "XAF","XAG","XAU","XBA","XBB","XBC","XBD","XCD","XDR","XOF",
        "XPD","XPF","XPT","XSU","XTS","XUA","XXX", "YER", "ZAR","ZMW","ZWL"
    ];

    // ----------------------------
    // NATIONAL ID
    // ----------------------------
    public static bool IsNationalIdValid(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return false;

        return id.All(char.IsDigit) && id.Length == 9;
    }

    // ----------------------------
    // SPOUSE ID
    // ----------------------------
    public static bool IsSpouseIdValid(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return true;

        return id.All(char.IsDigit) && id.Length == 9;
    }

    // ----------------------------
    // ORGANIZATION
    // ----------------------------
    public static bool IsOrganizationValid(string organization)
    {
        if (string.IsNullOrWhiteSpace(organization))
            return false;

        return true;

        // return AllowedOrganizations.Contains(organization.Trim().ToUpper());
    }

    // ----------------------------
    // MODALITY
    // ----------------------------
    public static bool IsModalityValid(string modality)
    {
        if (string.IsNullOrWhiteSpace(modality))
            return false;

        return AllowedActivities.Contains(modality.Trim().ToUpper());
    }

    // ----------------------------
    // CURRENCY (ISO-4217 simplified)
    // ----------------------------
    public static bool IsCurrencyValid(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            return false;

        return AllowedCurrencies.Contains(currency.Trim().ToUpper());
    }

    // ----------------------------
    // FREQUENCY
    // ----------------------------
    public static bool IsFrequencyValid(string frequency, out int value)
    {
        value = default;

        if (string.IsNullOrWhiteSpace(frequency))
            return false;

        return int.TryParse(frequency, out value) && value > 0;
    }

    // ----------------------------
    // AMOUNT
    // ----------------------------
    public static bool IsAmountValid(string amount, out decimal value)
    {
        value = default;

        if (string.IsNullOrWhiteSpace(amount))
            return false;

        var cleaned = amount.Replace(",", "").Trim();

        return decimal.TryParse(cleaned, out value) && value > 0;
    }

    // ----------------------------
    // DATE FORMAT (YYYYMMDD)
    // ----------------------------
    public static bool IsDateValid(string dateString, out DateTime date)
    {
        date = default;

        if (string.IsNullOrWhiteSpace(dateString))
            return false;

        if (dateString.Length != 8 || !dateString.All(char.IsDigit))
            return false;

        return DateTime.TryParseExact(
            dateString,
            "yyyyMMdd",
            null,
            System.Globalization.DateTimeStyles.None,
            out date
        );
    }

    // ----------------------------
    // DATE RANGE: START <= END
    // ----------------------------
    public static bool IsDateRangeValid(DateTime start, DateTime end)
    {
        return end >= start;
    }
}
