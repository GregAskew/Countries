namespace Countries.DomainModel {

    public interface ICountryTimeZoneInfo {

        string CountryISO2 { get; }
        string CountryName { get; }
        bool DST { get; }
        string TimeZoneAcronym { get; }
        string TimeZoneName { get; }
        decimal UTCOffset { get; }

        string ToCSVString();
    }
}