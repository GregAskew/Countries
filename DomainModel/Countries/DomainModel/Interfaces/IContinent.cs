namespace Countries.DomainModel {

    #region Usings
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    #endregion

    public interface IContinent : IEntityBase {

        string Abbreviation { get; set; }
        ICollection<Country> Countries { get; set; }
        int Id { get; set; }
        string Name { get; set; }

        string ToCSVString();
    }
}