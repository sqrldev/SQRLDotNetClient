using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using ReactiveUI;
using SQRLDotNetClientUI.Views;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SQRLDotNetClientUI.ViewModels
{
    /// <summary>
    /// This Class is used as the binding model for the MessageBoxWindow 
    /// </summary>
    public class MessageBoxViewModel : ViewModelBase
    {
        private AutoResetEvent _buttonClicked = null;
        private ViewModelBase _parent;
        private MessageBoxButtons _messageBoxButtons;
        private MessagBoxDialogResult _result;
        private string _message = "";
        private string _internalIcon { get; set; } = "resm:SQRLDotNetClientUI.Assets.Icons.ok.png";

        private MessageBoxButtonCustom[] _customButtons;

        /// <summary>
        /// Gets or sets the message to be displayed.
        /// </summary>
        public string Message
        {
            get => _message;
            set => this.RaiseAndSetIfChanged(ref _message, value);
        }

        /// <summary>
        /// Determines the width of the form (obsolete).
        /// </summary>
        public int Width { get; set; } = 400;

        /// <summary>
        /// Gets or sets the icon to be displayed.
        /// </summary>
        public Avalonia.Media.Imaging.Bitmap IconSource { get; set; }

        /// <summary>
        /// Instanciates a new <c>MessageBoxViewModel</c>.
        /// </summary>
        public MessageBoxViewModel()
        {
            Init();
        }

        /// <summary>
        /// Instanciates a new <c>MessageBoxViewModel</c>.
        /// </summary>
        /// <param name="title">MessageBox header title to be displayed.</param>
        /// <param name="message">Actual message to be displayed in the MessageBox.</param>
        /// <param name="messageBoxSize">MessageBox size (width), default is "Medium".</param>
        /// <param name="messageBoxButtons">MessageBox button combiniation to display (default is "OK").</param>
        /// <param name="messageBoxIcon">MessageBox icon to display (default is "OK").</param>
        public MessageBoxViewModel(string title, string message, MessageBoxSize messageBoxSize = MessageBoxSize.Medium, 
            MessageBoxButtons messageBoxButtons = MessageBoxButtons.OK, MessageBoxIcons messageBoxIcon = MessageBoxIcons.OK, MessageBoxButtonCustom[] customButtons =null)
        {
            _buttonClicked = new AutoResetEvent(false);
            this.Title = title;
            this.Message = message;
            this._messageBoxButtons = messageBoxButtons;
            this._customButtons = customButtons;
            _internalIcon = messageBoxIcon switch
            {
                MessageBoxIcons.ERROR => "resm:SQRLDotNetClientUI.Assets.Icons.error.png",
                MessageBoxIcons.WARNING => "resm:SQRLDotNetClientUI.Assets.Icons.warning.png",
                MessageBoxIcons.QUESTION => "resm:SQRLDotNetClientUI.Assets.Icons.question.png",
                _ => "resm:SQRLDotNetClientUI.Assets.Icons.ok.png",
            };

            Init();
        }

        /// <summary>
        /// Performs a few initialization tasks.
        /// </summary>
        private void Init()
        {
            var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
            this.IconSource = new Avalonia.Media.Imaging.Bitmap(assets.Open(new Uri(_internalIcon)));
        }

        /// <summary>
        /// Adds a button to the pnlButtons panel in the message box
        /// </summary>
        /// <param name="name">name of the button</param>
        /// <param name="content">text to be shown on button</param>
        /// <param name="clickResult">Result to return when button is clicked</param>
        /// <param name="isDefault">If set to <c>true</c>, the button will be marked as the default
        /// button for the current form.</param>
        private void AddButton(string name, string content, MessagBoxDialogResult clickResult, bool isDefault = false)
        {
            var msgWindow = AvaloniaLocator.Current.GetService<MessageBoxView>();
            var panel = msgWindow.FindControl<StackPanel>("pnlButtons").Children;
            Button btn = new Button
            {
                Content = content,
                Name = name,
                Margin = new Thickness(10, 0),
                MinWidth = 60,
                IsDefault = isDefault
            };
            
            btn.Click += (s, e) => 
            {
                ((MainWindowViewModel)this._mainWindow.DataContext).Content = _parent;
                _result = clickResult;
                _buttonClicked.Set();
            };

            panel.Add(btn);
        }

        /// <summary>
        /// Adds the selected combination of buttons to the UI.
        /// </summary>
        public void AddButtons()
        {
            switch (_messageBoxButtons)
            {
                case MessageBoxButtons.OKCancel:
                    {
                        AddButton("OK", _loc.GetLocalizationValue("BtnOK"), MessagBoxDialogResult.OK, isDefault: true);
                        AddButton("Cancel", _loc.GetLocalizationValue("BtnCancel"), MessagBoxDialogResult.CANCEL);
                    }
                    break;
                case MessageBoxButtons.YesNo:
                    {
                        AddButton("Yes", _loc.GetLocalizationValue("BtnYes"), MessagBoxDialogResult.YES, isDefault: true);
                        AddButton("No", _loc.GetLocalizationValue("BtnNo"), MessagBoxDialogResult.NO);
                    }
                    break;
                case MessageBoxButtons.Custom:
                    {
                        foreach(var btn in _customButtons)
                        {
                            AddButton(Guid.NewGuid().ToString(),btn.Label, btn.DialogResult, btn.IsDefault);
                        }
                    }
                    break;
                default:
                    {
                        AddButton("OK", _loc.GetLocalizationValue("BtnOK"), MessagBoxDialogResult.OK, isDefault: true);
                    }
                    break;
            }
        }

        /// <summary>
        /// Displays the message box screen and waits for the user to click a button,
        /// returning the result.
        /// </summary>
        /// <param name="parent">The view model calling the message box.</param>
        public async Task<MessagBoxDialogResult> ShowDialog(ViewModelBase parent)
        {
            this._parent = parent;
            ((MainWindowViewModel)this._mainWindow.DataContext).Content = this;

            return await Task.Run(() =>
            {
                _buttonClicked.WaitOne();
                return _result;
            });
        }
    }

    public class  MessageBoxButtonCustom
    {
        public string Label { get; set; }

        public MessagBoxDialogResult DialogResult { get; set; }

        public bool IsDefault { get; set; }

        public MessageBoxButtonCustom(string label, MessagBoxDialogResult result, bool isDefault =false)
        {
            this.Label = label;
            this.DialogResult = result;
            this.IsDefault = isDefault;
        }
    }
}
