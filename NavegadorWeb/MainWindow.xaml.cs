<Window x:Class="NavegadorWeb.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:wv2="clr-namespace:Microsoft.Web.WebView2.Wpf;assembly=Microsoft.Web.WebView2.Wpf"
        mc:Ignorable="d"
        Title="Mi Navegador Web" Height="768" Width="1366"
        WindowStartupLocation="CenterScreen">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/> <RowDefinition Height="*"/>    </Grid.RowDefinitions>

        <Border Grid.Row="0" BorderBrush="LightGray" BorderThickness="0,0,0,1">
            <StackPanel Orientation="Vertical">
                <StackPanel Orientation="Horizontal" Margin="5">
                    <Button x:Name="BackButton" Content="Atrás" Width="60" Margin="0,0,5,0" Click="BackButton_Click"/>
                    <Button x:Name="ForwardButton" Content="Adelante" Width="60" Margin="0,0,5,0" Click="ForwardButton_Click"/>
                    <Button x:Name="RefreshButton" Content="Recargar" Width="70" Margin="0,0,5,0" Click="RefreshButton_Click"/>
                    <TextBox x:Name="UrlTextBox" VerticalContentAlignment="Center" KeyDown="UrlTextBox_KeyDown" MinWidth="400" HorizontalAlignment="Stretch" Text="https://www.google.com" Margin="0,0,5,0"/>
                    <Button x:Name="GoButton" Content="Ir" Width="40" Margin="0,0,5,0" Click="GoButton_Click"/>
                    <Button x:Name="HomeButton" Content="Inicio" Width="60" Margin="0,0,5,0" Click="HomeButton_Click"/>
                    <Button x:Name="SettingsButton" Content="Opciones" Width="80" Click="SettingsButton_Click"/>
                </StackPanel>

                <TabControl x:Name="BrowserTabControl" SelectionChanged="BrowserTabControl_SelectionChanged" Margin="5,0,5,5">
                    <TabControl.Resources>
                        <Style TargetType="TabItem">
                            <Setter Property="HeaderTemplate">
                                <Setter.Value>
                                    <DataTemplate>
                                        <StackPanel Orientation="Horizontal">
                                            <ContentPresenter Content="{Binding RelativeSource={RelativeSource Mode=FindAncestor, AncestorType={x:Type TabItem}}, Path=Header}"/>
                                            <Button Content="X" Margin="5,0,0,0"
                                                    Style="{StaticResource TabCloseButtonStyle}"
                                                    Tag="{Binding RelativeSource={RelativeSource Mode=FindAncestor, AncestorType={x:Type TabItem}}}"/>
                                        </StackPanel>
                                    </DataTemplate>
                                </Setter.Value>
                            </Setter>
                        </Style>
                        <Style x:Key="TabCloseButtonStyle" TargetType="Button">
                            <Setter Property="Template">
                                <Setter.Value>
                                    <ControlTemplate TargetType="Button">
                                        <Border Background="Transparent" Padding="3">
                                            <TextBlock Text="{TemplateBinding Content}" FontSize="10" FontWeight="Bold" Foreground="Gray"/>
                                        </Border>
                                    </ControlTemplate>
                                </Setter.Value>
                            </Setter>
                            <Setter Property="Cursor" Value="Hand"/>
                            <EventSetter Event="Click" Handler="CloseTabButton_Click"/>
                        </Style>
                    </TabControl.Resources>

                    <TabItem Header="Nueva Pestaña">
                        <Grid>
                            <wv2:WebView2 x:Name="InitialWebView" Source="https://www.google.com"/>
                        </Grid>
                    </TabItem>

                    <TabItem Header="+" IsEnabled="False" Margin="0,0,-20,0">
                        <TabItem.Template>
                            <ControlTemplate TargetType="{x:Type TabItem}">
                                <Border Background="Transparent" BorderBrush="Transparent" Margin="0" Padding="0">
                                    <Button x:Name="AddTabButton" Content="+" Padding="5,0" Margin="0"
                                            BorderThickness="0" Background="Transparent"
                                            VerticalContentAlignment="Center" HorizontalContentAlignment="Center"
                                            Click="AddTabButton_Click"/>
                                </Border>
                            </ControlTemplate>
                        </TabItem.Template>
                    </TabItem>
                </TabControl>
            </StackPanel>
        </Border>

        </Grid>
</Window>
