using System;
using System.Net;
using GitCommands;

namespace ResourceManager
{
    public class CommitInformation
    {
        /// <summary>
        /// Private constructor
        /// </summary>
        private CommitInformation(string header, string body)
        {
            Header = header;
            Body = body;
        }

        public string Header {get; private set;}
        public string Body {get; private set;}

        /// <summary>
        /// Gets the commit info for module.
        /// </summary>
        /// <param name="module">Git module.</param>
        /// <param name="sha1">The sha1.</param>
        /// <returns></returns>
        public static CommitInformation GetCommitInfo(GitModule module, string sha1)
        {
            string error = "";
            CommitData data = CommitData.GetCommitData(module, sha1, ref error);
            if (data == null)
                return new CommitInformation(error, "");

            string header = data.GetHeader(false);
            string body = "\n" + WebUtility.HtmlEncode(data.Body.Trim());

            return new CommitInformation(header, body);
        }

        /// <summary>
        /// Gets the commit info from CommitData.
        /// </summary>
        /// <returns></returns>
        public static CommitInformation GetCommitInfo(CommitData data, bool showRevisionsAsLinks, GitModule module = null)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            string header = data.GetHeader(showRevisionsAsLinks);
            string body = "\n" + WebUtility.HtmlEncode(data.Body.Trim());

            if (showRevisionsAsLinks)
                body = GitRevision.Sha1HashShortRegex.Replace(body, match => ProcessHashCandidate(module, hash: match.Value));
            return new CommitInformation(header, body);
        }

        private static string ProcessHashCandidate(GitModule module, string hash)
        {
            if (module == null)
                return hash;
            string fullHash;
            if (!module.IsExistingCommitHash(hash, out fullHash))
                return hash;
            return LinkFactory.CreateCommitLink(guid: fullHash, linkText: hash, preserveGuidInLinkText: true);
        }
    }
}