# Lipis.Flags.Avalonia
A simple library for showing flags in Avalonia.

Lipis.Flags.Avalonia provides an Avalonia IValueConverter that can be used to convert an ISO 3166-1 alpha-2 country code into an SVG that can be used in Avalonia applications.

It is based on FamFamFam.Flags.Wpf, from which it lifts part of its internal workings.

## Installation
Source from [NuGet](https://www.nuget.org/packages/Lipis.Flags.Avalonia/):

> Install-Package Lipis.Flags.Avalonia

## Usage
The following code creates an image of flag that corresonds to the [two letter ISO 3166-1 alpha-2 country
code](https://en.wikipedia.org/wiki/ISO_3166-1_alpha-2) that is resolved by the binding expression.
Note that you may have to include a path in your binding, depending upon the structure of your `DataContext`.

```xaml
<Svg Source="{Binding Converter={StaticResource CountryIdToFlagImageSourceConverter}}" />
```

The converter in this binding transforms the country code into an `Svg`.
This requires an instance of the converter class to be within scope as a resource.

You might use code such as this somewhere up the logical tree:

```xaml
<Flags:CountryIdToFlagImageSourceConverter x:Key="CountryIdToFlagImageSourceConverter" />
```

You could show a `ComboBox` of country flags and names using the following code:

```xaml
<ComboBox ItemsSource="{Binding Source={x:Static flags:CountryData.AllCountries}}">
  <ComboBox.ItemTemplate>
    <DataTemplate DataType="flags:CountryData">
      <StackPanel Orientation="Horizontal">
        <Image Source="{Binding Path=Iso2, Converter={StaticResource CountryIdToFlagImageSourceConverter}}"
               Stretch="None" Width="23" Height="18" RenderOptions.BitmapScalingMode="HighQuality" />
        <TextBlock Text="{Binding Path=Name}" Margin="5,0,0,0" VerticalAlignment="Center" />
      </StackPanel>
    </DataTemplate>
  </ComboBox.ItemTemplate>
</ComboBox>
```

Lipis.Flags.Avalonia supports both 4:3 (the default) and 1:1 aspect ratios. This is controlled by passing a `FlagAspectRatio` value as a converter parameter, like this:

```xaml
<Svg Source="{Binding Converter={StaticResource CountryIdToFlagImageSourceConverter}, ConverterParameter={x:Static flags:FlagAspectRatio.OneByOne}}" />
```

## Credits

The Lipis flag icons were created by [Panayiotis Lipiridis](https://lipis.dev/).

The original FamFamFam.Flags.Wpf was created by [Drew Noakes](https://github.com/drewnoakes)