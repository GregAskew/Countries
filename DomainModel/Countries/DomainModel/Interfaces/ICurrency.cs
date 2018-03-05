namespace Countries.DomainModel {

    using System.Collections.Generic;

    public interface ICurrency : IEntityBase {

        string Code { get; set; }
        int DecimalDigits { get; set; }
        int Id { get; set; }
        string Name { get; set; }
        byte[] RowVersion { get; set; }

        ICollection<Country> Countries { get; set; }

        string ToCSVString();
    }
}