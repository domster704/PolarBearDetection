﻿using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using PolatBearDetection.IO;
using PolatBearDetection.Tween;
using PolatBearDetection.Python;
using PolatBearDetection.Extensions;
using PolatBearDetection.Properties;
using PolatBearDetection.Configuration;
using PolatBearDetection.ControlWrappers;

namespace PolatBearDetection
{
    public partial class MainForm : Form
    {
        private readonly TransitionHandler _findSubmenuTransitionHandler;
        private readonly TransitionHandler _findButtonTransitionHandler;
        private readonly TransitionHandler _closeButtonTransitionHandler;

        private readonly PythonExecutor _imageConverter;

        private readonly BearFoundLabelWrapper _bearFoundLabelWrapper;

        private readonly IBearFilesConfiguration _bearFilesConfiguration;
        private readonly IPythonFilesConfiguration _pythonFilesConfiguration;

        public MainForm()
        {
            InitializeComponent();

            FormClosed += OnFormClosed;

            _bearFilesConfiguration = new DefaultBearFilesConfiguration();
            _pythonFilesConfiguration = new DefaultPythonFilesConfiguration();

            _findSubmenuTransitionHandler = new TransitionHandler(FindBearSubMenuTransition,
                                                                  FindSubmenuPanel,
                                                                  false);

            _findButtonTransitionHandler = new TransitionHandler(TransparentTransition, FindButton, false);
            _closeButtonTransitionHandler = new TransitionHandler(TransparentTransition, SaveResultImageButton, true);

            _imageConverter = new PythonImageConverter(_pythonFilesConfiguration);

            _bearFoundLabelWrapper = new BearFoundLabelWrapper(BearFoundLabel, new DefaultFindLabelStyle());
        }

        private void FindMenuButton_Click(object sender, EventArgs e)
        {
            _findSubmenuTransitionHandler.Process();
        }

        private void ChooseImageButton_Click(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog()
            {
                Filter = "Images | *.png; *.jpg; *.JPEG; *.bmp"
            };

            if (openFileDialog.ShowDialog() != DialogResult.OK)
                return;

            _bearFoundLabelWrapper.SetEmptyText();

            BearPictureBox.RefreshWithImage(Resources.logo1);

            var photoSave = new BearPhotoSave(openFileDialog.FileName, _bearFilesConfiguration);
            photoSave.Save();

            _findButtonTransitionHandler.Show();
            _closeButtonTransitionHandler.Hide();
        }

        private async void FindButton_Click(object sender, EventArgs e)
        {
            BearPictureBox.RefreshWithImage(Resources.Preloader);

            await _imageConverter.ExecuteAsync().ContinueWith(task =>
            {
                var contains = Convert.ToBoolean(File.ReadAllText("Data/ContainsBear.txt"));

                BearFoundLabel.Text = contains ? "Медведь найден" : "Медведь не найден";

                if (contains)
                {
                    var photoSave = new BearPhotoSave(_bearFilesConfiguration);
                    photoSave.Save();

                    BearPictureBox.Image = new Bitmap(_bearFilesConfiguration.CopiedFileName);
                    _closeButtonTransitionHandler.Show();
                    _bearFoundLabelWrapper.SetBearFoundStyle();
                }
                else
                {
                    BearPictureBox.RefreshWithImage(Resources.Cross);
                    _bearFoundLabelWrapper.SetBearNotFoundStyle();
                    _closeButtonTransitionHandler.Hide();
                }
            });

            _findButtonTransitionHandler.Process();
        }

        private void SaveResultImageButton_Click(object sender, EventArgs e)
        {
            if (File.Exists(_bearFilesConfiguration.CopiedFileName) == false)
            {
                MessageBox.Show("Результат отсутсвует. Нажмите \"Найти\"");
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                FileName = "BearFile",
                Filter = "*.JPG | *.JPG;"
            };

            if (saveFileDialog.ShowDialog() != DialogResult.OK)
                return;

            var photoSave = new BearPhotoSave(_bearFilesConfiguration.CopiedFileName, saveFileDialog.FileName);
            photoSave.Save();
        }

        private void OnFormClosed(object sender, EventArgs e)
        {
            BearPictureBox.RefreshWithImage(Resources.logo1);

            var bearFiles = Directory.GetFiles("Data").Where(name => name.Contains("Bear"));

            foreach (var file in bearFiles)
                File.Delete(file);
        }
    }
}
