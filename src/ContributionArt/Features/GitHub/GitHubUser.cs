namespace ContributionArt.Features.GitHub
{
    /// <summary>A GitHub account.</summary>
    public sealed class GitHubUser
    {
        private readonly string _login;
        private readonly long _id;
        private readonly string? _name;

        public GitHubUser(string login, long id, string? name)
        {
            _login = login;
            _id = id;
            _name = name;
        }

        public string Login
        {
            get { return _login; }
        }

        public long Id
        {
            get { return _id; }
        }

        public string? Name
        {
            get { return _name; }
        }

        /// <summary>GitHub's private no-reply address. Commits with it always count toward the account.</summary>
        public string NoReplyEmail
        {
            get { return $"{_id}+{_login}@users.noreply.github.com"; }
        }
    }
}
