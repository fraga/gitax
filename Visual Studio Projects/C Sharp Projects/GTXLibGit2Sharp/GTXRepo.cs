using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;

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
                var indexPath = fileInfo.FullName.Replace(repo.Info.WorkingDirectory, "");
                var commits = repo.Head.Commits.Where(c => c.Parents.Count() == 1 && c.Tree[indexPath] != null && (c.Parents.FirstOrDefault().Tree[indexPath] == null || c.Tree[indexPath].Target.Id != c.Parents.FirstOrDefault().Tree[indexPath].Target.Id));
                
                foreach (Commit commit in commits)
                {
                    tmpItem.User = commit.Author.ToString();
                    tmpItem.GTXShaShort = commit.Sha.Substring(0, 7);
                    tmpItem.GTXSha = commit.Sha;
                    tmpItem.Comment = commit.Message;
                    tmpItem.ShortComment = commit.MessageShort;
                    tmpItem.VCSDate = commit.Committer.When.Date;
                    tmpItem.Filename_ = fileInfo.FullName;
                    tmpItem.InternalFilename = fileInfo.FullName;
                    tmpItem.insert();
                }
            }
            return tmpItem;
        }

        /// <summary>
        /// Get a single file version from the git repository
        /// </summary>
        /// <param name="repoPath">Main repository folder</param>
        /// <param name="tmpItem">The temporary item table holding the sha commit</param>
        /// <returns>a temporary file path</returns>
        public static string FileGetVersion(string repoPath, string fileName, SysVersionControlTmpItem tmpItem)
        {
            string indexPath = tmpItem.InternalFilename.Replace(repoPath, string.Empty);
            
            CheckoutOptions options = new CheckoutOptions();
            options.CheckoutModifiers = CheckoutModifiers.Force;

            using (Repository repo = new Repository(repoPath))
            {
                var commit = repo.Lookup<Commit>(tmpItem.GTXSha);
                if (commit != null)
                {
                    try
                    {
                        repo.CheckoutPaths(commit.Id.Sha, new string[] { fileName }, options);
                    }
                    catch (MergeConflictException ex)
                    {
                        //should not reach here as we're forcing checkout
                        throw ex;
                    }
                    
                }
            }

            return fileName;
        }
    }
}