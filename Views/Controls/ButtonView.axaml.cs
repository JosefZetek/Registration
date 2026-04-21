using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Markup.Xaml;

namespace Registration.Views
{
    public partial class ButtonView : UserControl
    {
        public event EventHandler? Click;

        public string Text
        {
            get => PART_Text.Text;
            set => PART_Text.Text = value;
        }

        public IImage? Icon
        {
            get => PART_Icon.Source;
            set => PART_Icon.Source = value;
        }

        public ButtonView()
        {
            InitializeComponent();

            PART_Button.Click += (_, __) => Click?.Invoke(this, EventArgs.Empty);
        }
    }
}