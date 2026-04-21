using Avalonia.Controls;
using Avalonia.Media;

namespace Registration.Views
{
    public partial class IndicatorView : UserControl
    {
        public string Text
        {
            get => Indicator_Text.Text;
            set => Indicator_Text.Text = value;
        }

        public IImage? Icon
        {
            get => Indicator_Icon.Source;
            set => Indicator_Icon.Source = value;
        }

        public double Opacity
        {
            get => Indicator_Element.Opacity;
            set => Indicator_Element.Opacity = value;

        }

        public IndicatorView()
        {
            InitializeComponent();
        }
    }
}