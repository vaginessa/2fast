﻿using Microsoft.Toolkit.Mvvm.Input;
using Prism.Commands;
using Prism.Ioc;
using Project2FA.Core.Services.JSON;
using Project2FA.Repository.Models;
using Project2FA.Strings;
using Project2FA.Uno.Core.File;
using Project2FA.Uno.Core.Serialization;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
#if HAS_WINUI
using Microsoft.UI.Xaml.Controls;
using Microsoft.Storage;
using Microsoft.Storage.AccessCache;
using Microsoft.Storage.Pickers;
#else
using Windows.UI.Xaml.Controls;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
#endif


namespace Project2FA.ViewModels
{
    public class NewDataFilePageViewModel : DatafileViewModelBase
    {
        private IFileService FileService { get; }
        private ISerializationService SerializationService { get; }

        private string _errorText;

        /// <summary>
        /// Constructor
        /// </summary>
        public NewDataFilePageViewModel(
            IContainerProvider containerProvider,
            IFileService fileService,
            ISerializationService serializationService) :base(containerProvider)
        {
            SerializationService = serializationService;
            FileService = fileService;

            PrimaryButtonCommand = new AsyncRelayCommand(PrimaryButtonCommandTask);


            CheckServerAddressCommand = new AsyncRelayCommand(CheckServerStatus);

            ConfirmErrorCommand = new DelegateCommand(() =>
            {
                ShowError = false;
            });

            LoginCommand = new AsyncRelayCommand(CheckLoginAsync);

            ChangePathCommand = new AsyncRelayCommand(async () =>
            {
                await SetLocalPath(true); //change path is true
            });


            ChooseWebDAVCommand = new DelegateCommand(() =>
            {

            });
        }

        /// <summary>
        /// Checks the inputs and enables / disables the submit button
        /// </summary>
        public override async Task CheckInputs()
        {
            if (!string.IsNullOrEmpty(DateFileName))
            {
                if (!string.IsNullOrEmpty(Password) && !string.IsNullOrEmpty(PasswordRepeat))
                {
                    if (!await CheckIfNameExists(DateFileName + ".2fa"))
                    {
                        if (Password == PasswordRepeat)
                        {
                            DatafileBTNActive = true;
                        }
                        else
                        {
                            DatafileBTNActive = false;
                        }

                        //ShowError = true;
                        //ErrorText = "#Die Passwörter stimmen nicht überein";

                    }
                    else
                    {
                        ShowError = true;
                        ErrorText = Resources.NewDatafileContentDialogDatafileExistsError;
                    }
                }
                else
                {
                    DatafileBTNActive = false;
                }
            }
            else
            {
                DatafileBTNActive = false;
            }
        }

        private async Task PrimaryButtonCommandTask()
        {
            try
            {
                Password = Password.Replace(" ", string.Empty);
                await CreateLocalFileDB(false);
                byte[] iv = new AesManaged().IV;
                DatafileModel file = new DatafileModel() { IV = iv, Collection = new System.Collections.ObjectModel.ObservableCollection<TwoFACodeModel>() };
                await FileService.WriteStringAsync(DateFileName, SerializationService.Serialize(file), await StorageFolder.GetFolderFromPathAsync(LocalStorageFolder.Path));
                App.ShellPageInstance.NavigationIsAllowed = true;
            }
            catch (Exception exc)
            {
                if (exc is UnauthorizedAccessException)
                {

                }
                throw;
            }
        }

        /// <summary>
        /// Starts a folder picker to pick a datafile from a local path
        /// </summary>
        /// <returns>boolean</returns>
        public async Task<bool> SetLocalPath(bool changePath = false)
        {
            SelectedIndex = 1;
            var folderPicker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.ComputerFolder
            };
            folderPicker.FileTypeFilter.Add("*");
            IsLoading = true;
            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                IsLoading = false;
                LocalStorageFolder = folder;
                return true;
            }
            else
            {
                //prevents the change of the index, if the user want to change
                //the path, but cancel the dialog 
                IsLoading = false;
                if (!changePath)
                {
                    SelectedIndex = 0;
                }
                return false;
            }
        }

        public string ErrorText
        {
            get => _errorText;
            set
            {
                SetProperty(ref _errorText, value);
            }
        }
    }
}
