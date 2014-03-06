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

            //TODO: Dangerous, consider refactoring
            FileInfo fileInfo = new FileInfo(filePath);

            using (var repo = new Repository(repoPath))
            {
                var indexPath = fileInfo.FullName.Replace(repo.Info.WorkingDirectory, string.Empty);
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
        /// <param name="repoPath">Repository main path</param>
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

        /// <summary>
        /// Resets the changes of a file to it's HEAD last commit
        /// </summary>
        /// <param name="repoPath">Repository main path</param>
        /// <param name="fileName">The file path</param>
        /// <returns>True if reset was successful false if not</returns>
        public static bool FileUndoCheckout(string repoPath, string fileName)
        {
            //TODO: Dangerous, consider refactoring
            FileInfo fileInfo = new FileInfo(fileName);

            using (Repository repo = new Repository(repoPath))
            {
                string indexPath = fileInfo.FullName.Replace(repo.Info.WorkingDirectory, string.Empty);

                CheckoutOptions doForceCheckout = new CheckoutOptions();
                doForceCheckout.CheckoutModifiers = CheckoutModifiers.Force;

                var fileCommits = repo.Head.Commits.Where(c => c.Parents.Count() == 1 && c.Tree[indexPath] != null && (c.Parents.FirstOrDefault().Tree[indexPath] == null || c.Tree[indexPath].Target.Id != c.Parents.FirstOrDefault().Tree[indexPath].Target.Id));

                if (fileCommits.Any())
                {
                    var lastCommit = fileCommits.First();
                    repo.CheckoutPaths(lastCommit.Id.Sha, new string[] { fileName }, doForceCheckout);

                    return true;
                }
                else
                    return false;
            }
        }

        /// <summary>
        /// Check if the file exists or ever existed in the repository
        /// </summary>
        /// <param name="repoPath">Repository main path</param>
        /// <param name="filePath">full file path to be checking</param>
        /// <returns>True if the file exists in the repository</returns>
        public static bool FileExists(string repoPath, string filePath)
        {
            bool fileExisted;

            using (Repository repo = new Repository(repoPath))
            {
                string indexPath = filePath.Replace(repo.Info.WorkingDirectory, string.Empty);
                fileExisted = repo.Head.Commits.Where(c => c.Tree[indexPath] != null).Any();
            }

            return fileExisted;
        }

        /// <summary>
        /// Synchronizes a folder
        /// </summary>
        /// <param name="repoPath">Main repository path</param>
        /// <param name="folderPath">The folder to synchronize (checkout)</param>
        /// <param name="forceCheckout">Forces the update from the latest comit (head tip)</param>
        /// <returns>A SysVersionControlItem with all the files that have been affected</returns>
        public static SysVersionControlTmpItem FolderSync(string repoPath, string folderPath, bool forceCheckout)
        {
            SysVersionControlTmpItem tmpItem = new SysVersionControlTmpItem();
            CheckoutOptions checkoutOptions = new CheckoutOptions();
            checkoutOptions.CheckoutModifiers = forceCheckout ? CheckoutModifiers.Force : CheckoutModifiers.None;
            string tipSha;

            using (Repository repo = new Repository(repoPath))
            {
                repo.CheckoutPaths(repo.Head.Tip.Id.Sha, new string[] { folderPath }, checkoutOptions);
                tipSha = repo.Head.Tip.Id.Sha;
            }
            
            //TODO: We should get a list of files from the repository
            Directory.EnumerateFiles(folderPath, "*.xpo", SearchOption.AllDirectories).ToList().ForEach(f =>
            {
                tmpItem.ItemPath = "\\" + f.Replace(repoPath, string.Empty);
                tmpItem.GTXSha = tipSha;
                tmpItem.ActionText = "Update";
                tmpItem.insert();
            });

            return tmpItem;
        }

    }
}
