﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.Templates.Core;
using Microsoft.Templates.UI.Controls;
using Microsoft.Templates.UI.Extensions;
using Microsoft.Templates.UI.Mvvm;
using Microsoft.Templates.UI.Resources;
using Microsoft.Templates.UI.Services;
using Microsoft.Templates.UI.ViewModels.NewProject;

namespace Microsoft.Templates.UI.ViewModels.Common
{
    public class SavedTemplateViewModel : Observable
    {
        private string _name;
        private string _icon;
        private bool _itemNameEditable;
        private bool _isHidden;
        private bool _hasErrors;
        private bool _isReorderEnabled;
        private bool _isDragging;
        private bool _isFocused;
        private bool _isTextSelected;
        private ICommand _lostKeyboardFocusCommand;
        private ICommand _setFocusCommand;
        private Guid _id;

        public ITemplateInfo Template { get; }

        public string Identity { get; }

        public TemplateType TemplateType { get; }

        public int GenGroup { get; }

        public IEnumerable<BasicInfoViewModel> Dependencies { get; }

        public string Name
        {
            get => _name;
            set => SetName(value);
        }

        public string Icon
        {
            get => _icon;
            set => SetProperty(ref _icon, value);
        }

        public bool ItemNameEditable
        {
            get => _itemNameEditable;
            set => SetProperty(ref _itemNameEditable, value);
        }

        public bool IsHidden
        {
            get => _isHidden;
            set => SetProperty(ref _isHidden, value);
        }

        public bool HasErrors
        {
            get => _hasErrors;
            set => SetProperty(ref _hasErrors, value);
        }

        public bool IsDragging
        {
            get => _isDragging;
            set => SetProperty(ref _isDragging, value);
        }

        public bool IsReorderEnabled
        {
            get => _isReorderEnabled;
            set => SetProperty(ref _isReorderEnabled, value);
        }

        public bool IsFocused
        {
            get => _isFocused;
            set
            {
                if (_isFocused == value)
                {
                    SetProperty(ref _isFocused, false);
                }

                SetProperty(ref _isFocused, value);
            }
        }

        public bool IsTextSelected
        {
            get => _isTextSelected;
            set
            {
                if (_isTextSelected == value)
                {
                    SetProperty(ref _isTextSelected, false);
                }

                SetProperty(ref _isTextSelected, value);
            }
        }

        public TemplateOrigin TemplateOrigin { get; }

        public ICommand LostKeyboardFocusCommand => _lostKeyboardFocusCommand ?? (_lostKeyboardFocusCommand = new RelayCommand<KeyboardFocusChangedEventArgs>(OnLostKeyboardFocus));

        public ICommand SetFocusCommand => _setFocusCommand ?? (_setFocusCommand = new RelayCommand(() => IsFocused = true));

        public SavedTemplateViewModel(TemplateInfoViewModel template, TemplateOrigin templateOrigin)
        {
            _id = Guid.NewGuid();
            Template = template.Template;
            Identity = template.Identity;
            TemplateType = template.TemplateType;
            GenGroup = template.GenGroup;
            Dependencies = template.Dependencies;
            Icon = template.Icon;
            ItemNameEditable = template.ItemNameEditable;
            IsHidden = template.IsHidden;
            TemplateOrigin = templateOrigin;
            IsReorderEnabled = template.TemplateType == TemplateType.Page;
        }

        public void SetName(string newName, bool fromNewTemplate = false)
        {
            if (ItemNameEditable)
            {
                var validationResult = ValidationService.ValidateTemplateName(newName, ItemNameEditable, true);
                HasErrors = !validationResult.IsValid;
                MainViewModel.Instance.WizardStatus.HasValidationErrors = !validationResult.IsValid;
                if (validationResult.IsValid)
                {
                    NotificationsControl.CleanErrorNotificationsAsync(ErrorCategory.NamingValidation).FireAndForget();
                }
                else
                {
                    NotificationsControl.AddNotificationAsync(validationResult.GetNotification()).FireAndForget();
                }
            }

            SetProperty(ref _name, newName, nameof(Name));
            if (ValidationService.HasAllPagesViewSuffix(fromNewTemplate, newName))
            {
                var notification = Notification.Warning(string.Format(StringRes.NotificationValidationWarning_ViewSuffix, Configuration.Current.GitHubDocsUrl), Category.ViewSufixValidation, TimerType.Large);
                NotificationsControl.AddNotificationAsync(notification).FireAndForget();
            }
            else
            {
                NotificationsControl.CleanCategoryNotificationsAsync(Category.ViewSufixValidation).FireAndForget();
            }
        }

        private void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs args)
        {
            if (HasErrors)
            {
                var textBox = args.Source as TextBox;
                textBox.Focus();
            }
        }

        public override bool Equals(object obj)
        {
            var result = false;
            if (obj is SavedTemplateViewModel savedTemplate)
            {
                result = _id.Equals(savedTemplate._id);
            }
            else if (obj is TemplateInfoViewModel templateInfo)
            {
                result = Identity.Equals(templateInfo.Identity);
            }

            return result;
        }

        public override int GetHashCode() => base.GetHashCode();

        public ItemInfo GetUserSelection() => new ItemInfo() { Name = Name, Template = Template };
    }
}
