namespace LibiadaWeb.Models.Repositories.Catalogs
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The remote db repository.
    /// </summary>
    public class RemoteDbRepository : IRemoteDbRepository
    {
        /// <summary>
        /// The remote dbs.
        /// </summary>
        private readonly RemoteDb[] remoteDbs;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteDbRepository"/> class.
        /// </summary>
        /// <param name="db">
        /// The db.
        /// </param>
        public RemoteDbRepository(LibiadaWebEntities db)
        {
            remoteDbs = db.RemoteDb.ToArray();
        }

        /// <summary>
        /// The get select list with nature.
        /// </summary>
        /// <returns>
        /// The <see cref="IEnumerable{Object}"/>.
        /// </returns>
        public IEnumerable<SelectListItemWithNature> GetSelectListWithNature()
        {
            return remoteDbs.Select(n => new SelectListItemWithNature
            {
                Value = n.Id.ToString(),
                Text = n.Name,
                Selected = false,
                Nature = (byte)n.Nature
            });
        }

        /// <summary>
        /// The get select list with nature.
        /// </summary>
        /// <param name="selectedDb">
        /// The selected db.
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable{Object}"/>.
        /// </returns>
        public IEnumerable<SelectListItemWithNature> GetSelectListWithNature(int selectedDb)
        {
            return remoteDbs.Select(n => new SelectListItemWithNature
            {
                Value = n.Id.ToString(),
                Text = n.Name,
                Selected = n.Id == selectedDb,
                Nature = (byte)n.Nature
            });
        }

        /// <summary>
        /// The dispose.
        /// </summary>
        public void Dispose()
        {
        }
    }
}
