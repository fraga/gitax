using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibGit2Sharp;
using System.IO;

namespace GTXLibGit2Sharp
{
    /// <summary>
    /// LibGit2Sharp repository wrapper
    /// </summary>
    public static class GTXRepo
    {
        /// <summary>
        /// Initializes the git repository
        /// </summary>
        /// <param name="repoPath">the repository main path</param>
        public static void Init(string repoPath)
        {
            Repository.Init(repoPath);
        }

        /// <summary>
        /// Gets the libGit2Sharp version
        /// </summary>
        /// <returns>Libgit2Sharp version</returns>
        public static string Version()
        {
            return Repository.Version;
        }

        /// <summary>
        /// Brings all commits related to the file
        /// </summary>
        /// <param name="repoPath">Repository main path</param>
        /// <param name="filePath">File to be pulling the commits</param>
        /// <returns>A SysVersionControlTmpItem filled with the commits</returns>
        public static SysVersionControlTmpItem FileHistory(string repoPath, string filePath)
        {
            SysVersionControlTmpItem tmpItem = new SysVersionControlTmpItem();

            FileInfo fileInfo = new FileInfo(filePath);

            using (var repo = new Repository(repoPath))
            {
                var commits = repo.Head.Commits;
                
                foreach (Commit commit in commits)
                {
                    var trees = commit.Tree;

                    foreach (var tree in trees)
                    {
                        if (tree.Target is Tree && (tree.Target as Tree)[fileInfo.Name] != null)
                        {
                            tmpItem.User = commit.Author.ToString();
                            tmpItem.GTXSha = commit.Sha.Substring(0, 7);
                            tmpItem.Comment = commit.Message;
                            tmpItem.ShortComment = commit.MessageShort;
                            tmpItem.VCSDate = commit.Committer.When.Date;
                            tmpItem.insert();
                        }
                    }
                }
            }
            return tmpItem;
        }
    }
}
