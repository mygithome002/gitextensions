﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GitCommands;
using ResourceManager;

namespace GitUI.CommandsDialogs
{
    public sealed partial class FormGitIgnore : GitModuleForm
    {
        private readonly TranslationString _editLocalExcludeTitle =
            new TranslationString("Edit .git/info/exclude");

        private readonly TranslationString _gitignoreOnlyInWorkingDirSupported =
            new TranslationString(".gitignore is only supported when there is a working directory.");
        private readonly TranslationString _gitignoreOnlyInWorkingDirSupportedCaption =
            new TranslationString("No working directory");

        private readonly TranslationString _cannotAccessGitignore =
            new TranslationString("Failed to save .gitignore." + Environment.NewLine + "Check if file is accessible.");
        private readonly TranslationString _cannotAccessGitignoreCaption =
            new TranslationString("Failed to save .gitignore");

        private readonly TranslationString _saveFileQuestion =
            new TranslationString("Save changes to .gitignore?");
        private readonly TranslationString _saveFileQuestionCaption =
            new TranslationString("Save changes?");

        private bool _localExclude;
        private string _originalGitIgnoreFileContent = string.Empty;

        #region default patterns
        private static readonly string DefaultIgnorePatternsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GitExtensions/DefaultIgnorePatterns.txt");
        private static readonly string[] DefaultIgnorePatterns = new[]
        {
            "#Ignore thumbnails created by Windows",
            "Thumbs.db",
            "#Ignore files built by Visual Studio",
            "*.obj",
            "*.exe",
            "*.pdb",
            "*.user",
            "*.aps",
            "*.pch",
            "*.vspscc",
            "*_i.c",
            "*_p.c",
            "*.ncb",
            "*.suo",
            "*.tlb",
            "*.tlh",
            "*.bak",
            "*.cache",
            "*.ilk",
            "*.log",
            "[Bb]in",
            "[Dd]ebug*/",
            "*.lib",
            "*.sbr",
            "obj/",
            "[Rr]elease*/",
            "_ReSharper*/",
            "[Tt]est[Rr]esult*",
            ".vs/",
            "#Nuget packages folder",
            "packages/"
        };
        #endregion

        public FormGitIgnore(GitUICommands aCommands, bool localExclude)
            : base(aCommands)
        {
            _localExclude = localExclude;
            InitializeComponent();
            Translate();

            if (localExclude)
                Text = _editLocalExcludeTitle.Text;
        }

        private string ExcludeFile
        {
            get
            {
                if (!_localExclude)
                {
                    return Path.Combine(Module.WorkingDir, ".gitignore");
                }
                else
                {
                    return Path.Combine(Module.GetGitDirectory(), "info", "exclude");
                }
            }
        }

        protected override void OnRuntimeLoad(EventArgs e)
        {
            base.OnRuntimeLoad(e);
            LoadGitIgnore();
            _NO_TRANSLATE_GitIgnoreEdit.TextLoaded += GitIgnoreFileLoaded;
        }

        private void LoadGitIgnore()
        {
            try
            {
                if (File.Exists(ExcludeFile))
                    _NO_TRANSLATE_GitIgnoreEdit.ViewFile(ExcludeFile);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        private void SaveClick(object sender, EventArgs e)
        {
            SaveGitIgnore();
            Close();
        }

        private bool SaveGitIgnore()
        {
            if (!HasUnsavedChanges())
                return false;
            try
            {
                FileInfoExtensions
                    .MakeFileTemporaryWritable(
                        ExcludeFile,
                        x =>
                        {
                            var fileContent = _NO_TRANSLATE_GitIgnoreEdit.GetText();
                            if (!fileContent.EndsWith(Environment.NewLine))
                                fileContent += Environment.NewLine;
                            File.WriteAllBytes(x, GitModule.SystemEncoding.GetBytes(fileContent));
                            _originalGitIgnoreFileContent = fileContent;
                        });
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, _cannotAccessGitignore.Text + Environment.NewLine + ex.Message,
                    _cannotAccessGitignoreCaption.Text);
                return false;
            }
        }

        private void FormGitIgnoreFormClosing(object sender, FormClosingEventArgs e)
        {
            if (HasUnsavedChanges())
            {
                switch (MessageBox.Show(this, _saveFileQuestion.Text, _saveFileQuestionCaption.Text,
                                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question))
                {
                    case DialogResult.Yes:
                        if (!SaveGitIgnore())
                        {
                            e.Cancel = true;
                            return;
                        }
                        break;
                    case DialogResult.Cancel:
                        e.Cancel = true;
                        return;
                }
            }
        }

        private void FormGitIgnoreLoad(object sender, EventArgs e)
        {
            if (!Module.IsBareRepository())
                return;
            MessageBox.Show(this, _gitignoreOnlyInWorkingDirSupported.Text, _gitignoreOnlyInWorkingDirSupportedCaption.Text);
            Close();
        }

        private void AddDefaultClick(object sender, EventArgs e)
        {
            var defaultIgnorePatterns = (File.Exists(DefaultIgnorePatternsFile)) ? File.ReadAllLines(DefaultIgnorePatternsFile) : DefaultIgnorePatterns;

            var currentFileContent = _NO_TRANSLATE_GitIgnoreEdit.GetText();
            var patternsToAdd = defaultIgnorePatterns
                .Except(currentFileContent.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                .ToArray();
            if (patternsToAdd.Length == 0)
                return;
            // workaround to prevent GitIgnoreFileLoaded event handling (it causes wrong _originalGitIgnoreFileContent update)
            // TODO: implement in FileViewer separate events for loading text from file and for setting text directly via ViewText
            _NO_TRANSLATE_GitIgnoreEdit.TextLoaded -= GitIgnoreFileLoaded;
            _NO_TRANSLATE_GitIgnoreEdit.ViewText(ExcludeFile,
                currentFileContent + Environment.NewLine +
                string.Join(Environment.NewLine, patternsToAdd) + Environment.NewLine + string.Empty);
            _NO_TRANSLATE_GitIgnoreEdit.TextLoaded += GitIgnoreFileLoaded;
        }

        private void AddPattern_Click(object sender, EventArgs e)
        {
            SaveGitIgnore();
            UICommands.StartAddToGitIgnoreDialog(this, _localExclude, "*.dll");
            LoadGitIgnore();
        }

        private bool HasUnsavedChanges()
        {
            return _originalGitIgnoreFileContent != _NO_TRANSLATE_GitIgnoreEdit.GetText();
        }

        private void GitIgnoreFileLoaded(object sender, EventArgs e)
        {
            _originalGitIgnoreFileContent = _NO_TRANSLATE_GitIgnoreEdit.GetText();
        }

        private void lnkGitIgnorePatterns_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(@"https://github.com/github/gitignore");
        }

        private void lnkGitIgnoreGenerate_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(@"https://www.gitignore.io/");
        }
    }
}