using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using System.Reflection;

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
        /// Combines GTX and libGit2Sharp version
        /// </summary>
        /// <returns>Combined GTX and Libgit2Sharp version</returns>
        public static string Version()
        {
            return String.Format("GitAx {0} - libGit2Sharp {1}", GetGTXVersion(), GetLibGit2SharpVersion());
        }

        /// <summary>
        /// Gets the LibGit2Sharp version
        /// </summary>
        /// <returns>LibGit2Sharp version</returns>
        public static string GetLibGit2SharpVersion()
        {
            return Repository.Version;
        }

        /// <summary>
        /// Get the GTXLibGit2Sharp version
        /// </summary>
        /// <returns>GTXLibGit2Sharp assembly version</returns>
        public static string GetGTXVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version.ToString();
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
            try
            {
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
                        tmpItem.VCSTime = (int)commit.Committer.When.DateTime.TimeOfDay.TotalSeconds;
                        tmpItem.Filename_ = FileGetVersion(repoPath, fileInfo.FullName, commit.Sha, Path.Combine(Path.GetTempPath(), commit.Sha + fileInfo.Extension));
                        tmpItem.InternalFilename = fileInfo.FullName;
                        tmpItem.ItemPath = indexPath;
                        tmpItem.GTXFileRepoStatus = GetFileStatus(repoPath, fileInfo.FullName);
                        tmpItem.insert();
                    }
                }
            }
            catch (IOException ex)
            {
                throw ex;
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
                        repo.CheckoutPaths(commit.Id.Sha, new [] { fileName }, options);
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
        /// Gets a version from an SHA and saves it to another folder
        /// </summary>
        /// <param name="repoPath">Repository main path</param>
        /// <param name="sha">Git SHA</param>
        /// <param name="destinationPath">The destination path</param>
        /// <returns>Destination path</returns>
        public static string FileGetVersion(string repoPath, string fileName, string sha, string destinationPath)
        {
            using (Repository repo = new Repository(repoPath))
            {
                string indexPath = fileName.Replace(repo.Info.WorkingDirectory, string.Empty);

                var commit = repo.Lookup<Commit>(sha);
                
                Blob blob = null;

                if (commit != null)
                    blob = (Blob)commit.Tree[indexPath].Target;
                else
                    blob = (Blob)repo.Lookup(sha, ObjectType.Blob);

                using (StreamWriter writer = new StreamWriter(destinationPath))
                {
                    writer.Write(blob.GetContentText(Encoding.UTF8));
                }
            }

            return destinationPath;
        }

        /// <summary>
        /// Resets the changes of a file to it's HEAD last commit
        /// </summary>
        /// <param name="repoPath">Repository main path</param>
        /// <param name="fileName">The file path</param>
        /// <returns>True if reset was successful false if not</returns>
        public static bool FileUndoCheckout(string repoPath, string fileName, bool forceCheckout)
        {
            //TODO: Dangerous, consider refactoring
            FileInfo fileInfo = new FileInfo(fileName);

            using (Repository repo = new Repository(repoPath))
            {
                string indexPath = fileInfo.FullName.Replace(repo.Info.WorkingDirectory, string.Empty);

                CheckoutOptions checkoutOptions = new CheckoutOptions
                {
                    CheckoutModifiers =
                        forceCheckout
                            ? CheckoutModifiers.Force
                            : CheckoutModifiers.None
                };

                var fileCommits = repo.Head.Commits.Where(c => c.Parents.Count() == 1 &&
                                                          c.Tree[indexPath] != null &&
                                                          (c.Parents.FirstOrDefault().Tree[indexPath] == null ||
                                                            c.Tree[indexPath].Target.Id != c.Parents.FirstOrDefault().Tree[indexPath].Target.Id)
                                                          );

                if (fileCommits.Any())
                {
                    var lastCommit = fileCommits.First();
                    repo.CheckoutPaths(lastCommit.Id.Sha, new[] { fileName }, checkoutOptions);
                }

                return true;
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
        /// <param name="forceCheckout">Forces the update from the latest commit (head tip)</param>
        /// <returns>A SysVersionControlItem with all the files that have been affected</returns>
        public static SysVersionControlTmpItem FolderSync(string repoPath, string folderPath, bool forceCheckout)
        {
            SysVersionControlTmpItem tmpItem = new SysVersionControlTmpItem();

            CheckoutOptions checkoutOptions = new CheckoutOptions
            {
                CheckoutModifiers =
                    forceCheckout
                        ? CheckoutModifiers.Force
                        : CheckoutModifiers.None
            };
            
            string tipSha;

            using (Repository repo = new Repository(repoPath))
            {
                repo.CheckoutPaths(repo.Head.Tip.Id.Sha, new[] { folderPath }, checkoutOptions);
                tipSha = repo.Head.Tip.Id.Sha;

                InitTmpItemFromTree(repoPath, repo.Head.Tip.Tree, ref tmpItem);

            }

            return tmpItem;
        }

        /// <summary>
        /// Gets the file status in the index.
        /// </summary>
        /// <param name="repoPath">Main repository path</param>
        /// <param name="fileName">The filename to check it's status</param>
        /// <returns>An integer representing the FileStatus</returns>
        public static GTXFileStatus GetFileStatus(string repoPath, string fileName)
        {
            int fileStatus;

            using (Repository repo = new Repository(repoPath))
            {
                string indexPath = fileName.Replace(repo.Info.WorkingDirectory, string.Empty);
                fileStatus = (int)repo.Index.RetrieveStatus(indexPath);
            }

            return GetGTXFileStatus(fileStatus);
        }

        /// <summary>
        /// Due to restrictions on enum in AX, we're getting GTXFileStatus enum type from here
        /// </summary>
        /// <param name="fileStatus">LibGit2Sharp standard fileStatus</param>
        /// <returns>GTXFileStatus according to LibGit2Sharp</returns>
        private static GTXLibGit2Sharp.GTXFileStatus GetGTXFileStatus(int fileStatus)
        {
            switch (fileStatus)
            {
                case 0: /* GIT_STATUS_CURRENT */
                    return GTXFileStatus.Unaltered;
                case 1 << 0: /* GIT_STATUS_INDEX_NEW */
                    return GTXFileStatus.Added;
                case 1 << 1: /* GIT_STATUS_INDEX_MODIFIED */
                    return GTXFileStatus.Staged;
                case 1 << 2: /* GIT_STATUS_INDEX_DELETED */
                    return GTXFileStatus.Removed;
                case 1 << 3: /* GIT_STATUS_INDEX_RENAMED */
                    return GTXFileStatus.RenamedInIndex;
                case 1 << 4: /* GIT_STATUS_INDEX_TYPECHANGE */
                    return GTXFileStatus.StagedTypeChange;
                case 1 << 7: /* GIT_STATUS_WT_NEW */
                    return GTXFileStatus.Untracked;
                case 1 << 8: /* GIT_STATUS_WT_MODIFIED */
                    return GTXFileStatus.Modified;
                case 1 << 9: /* GIT_STATUS_WT_DELETED */
                    return GTXFileStatus.Missing;
                case 1 << 10: /* GIT_STATUS_WT_TYPECHANGE */
                    return GTXFileStatus.TypeChanged;
                case 1 << 11: /* GIT_STATUS_WT_RENAMED */
                    return GTXFileStatus.RenamedInWorkDir;
                case 1 << 14: /* GIT_STATUS_IGNORED */
                    return GTXFileStatus.Ignored;
                default: /* (1 << 31) */
                    return GTXFileStatus.NonExistent;
            }
        }

        /// <summary>
        /// Gets all files that are dirty in index
        /// </summary>
        /// <param name="repoPath">Repository main path</param>
        /// <returns>A list of files that have status dirt in index</returns>
        public static SysVersionControlTmpItem GetFilesInIndex(string repoPath)
        {
            SysVersionControlTmpItem tmpItem = new SysVersionControlTmpItem();

            using (Repository repo = new Repository(repoPath))
            {
                if (!repo.Index.RetrieveStatus().IsDirty)
                    return tmpItem;

                var allDirtFiles = repo.Index.RetrieveStatus(new StatusOptions { Show = StatusShowOption.IndexAndWorkDir }).
                                        Where(t => t.State != FileStatus.Unaltered && t.State != FileStatus.Ignored);
                
                foreach (var dirtFile in allDirtFiles)
                {
                    FileInfo fileInfo = new FileInfo(Path.Combine(repoPath, dirtFile.FilePath));

                    IndexEntry indexEntry = repo.Index[dirtFile.FilePath];

                    //No index entry means new file, untracked content
                    if (indexEntry != null)
                    {
                        tmpItem.GTXShaShort = indexEntry.Id.Sha.Substring(0, 7);
                        tmpItem.GTXSha = indexEntry.Id.Sha;
                        tmpItem.Filename_ = FileGetVersion(repoPath, fileInfo.FullName, indexEntry.Id.Sha, Path.Combine(Path.GetTempPath(), indexEntry.Id.Sha + fileInfo.Extension));
                        tmpItem.InternalFilename = fileInfo.FullName;
                        tmpItem.ItemPath = indexEntry.Path;
                        tmpItem.GTXFileRepoStatus = GetFileStatus(repoPath, fileInfo.FullName);
                    }
                    else
                    {
                        var tempFileName = Path.Combine(Path.GetTempPath(), fileInfo.Name);

                        File.Copy(fileInfo.FullName, tempFileName, true);

                        tmpItem.Filename_ = tempFileName;
                        tmpItem.InternalFilename = fileInfo.FullName;
                        tmpItem.ItemPath = dirtFile.FilePath;
                        tmpItem.GTXFileRepoStatus = GetFileStatus(repoPath, fileInfo.FullName);
                    }
                    tmpItem.insert();
                }
                
            }

            return tmpItem;
        }

        /// <summary>
        /// Initializes a SysVersionControlTmpItem from a tree path
        /// </summary>
        /// <param name="repoPath">The main repo path</param>
        /// <param name="tree">The tree git to iterate from</param>
        /// <param name="tmpItem">ref to the SysVersionControlTmpItem</param>
        /// <param name="recursive">If true will go recursively to all tree entries found (in case of a dir)</param>
        public static void InitTmpItemFromTree(string repoPath, Tree tree, ref SysVersionControlTmpItem tmpItem, bool recursive = true)
        {
            if (tmpItem == null)
                tmpItem = new SysVersionControlTmpItem();

            foreach (var treeEntry in tree)
            {
                if (treeEntry.TargetType == TreeEntryTargetType.Tree && recursive)
                    InitTmpItemFromTree(repoPath, treeEntry.Target as Tree, ref tmpItem);

                if (treeEntry.Mode == Mode.Directory || treeEntry.Mode == Mode.ExecutableFile || treeEntry.Mode == Mode.GitLink)
                    continue;

                string fileName = Path.Combine(repoPath, treeEntry.Path);

                if (SysVersionControlTmpItem.isValidXPOFile(fileName))
                {
                    tmpItem.ItemPath = "\\" + treeEntry.Path.Replace(repoPath, string.Empty);
                    tmpItem.Filename_ = fileName;
                    tmpItem.InternalFilename = fileName;
                    tmpItem.insert();
                }

            }

        }

        /// <summary>
        /// Verifies if the relative path is the root folder
        /// </summary>
        /// <param name="repoPath">The main repo path</param>
        /// <param name="relativePath">Relative path to compare too</param>
        /// <returns>True if the relative path is the root git folder path</returns>
        public static bool IsRootFolder(string repoPath, string relativePath)
        {
            using (Repository repo = new Repository(repoPath))
            {
                return repo.Info.WorkingDirectory.Equals(relativePath);
            }

        }

    }
}
