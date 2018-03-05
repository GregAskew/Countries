namespace Countries.DomainModel {

    public interface ICountryCurrencyInfo {

        int CountryId { get; }
        string CountryISO2 { get; }
        string CountryISO3 { get; }
        string CountryISONumeric { get; }
        string CountryName { get; }
        string CurrencyCode { get; }
        int? CurrencyId { get; }
        string CurrencyName { get; }

        string ToCSVString();
    }
}