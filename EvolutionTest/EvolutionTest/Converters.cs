using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;
using static EvolutionTest.MainWindow;

namespace EvolutionTest
{
	public class BotColorConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			ColorModes mode = (ColorModes)values[0];
			Bot bot = (Bot)values[1];

			string attribute = parameter as string;

			/*
			<Ellipse.Fill>
				<MultiBinding Converter="{StaticResource botColorConverter}">
					<MultiBinding.ConverterParameter>Fill</MultiBinding.ConverterParameter>
					<Binding Path="ColorMode" />
					<Binding Path="Obj" RelativeSource="{RelativeSource AncestorType=UserControl}"/>
				</MultiBinding>
			</Ellipse.Fill>

			color = Color.FromArgb(
				bot.Background.A,
				Darker(bot.Background.R),
				Darker(bot.Background.G),
				Darker(bot.Background.B));
			*/

			Color color;

			switch (mode)
			{
				case ColorModes.Normal:
					color = bot.Background;
					break;

				case ColorModes.Predators:
					color = bot.IsPredator ? Colors.Red : Colors.Green;
					break;

				case ColorModes.Energy:
					break;
			}

			return new SolidColorBrush(color);
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		private byte Darker(byte part)
		{
			int color = part;
			color += 50;
			if (color > 255)
			{
				color = 255;
			}

			return (byte)color;
		}
	}
}
