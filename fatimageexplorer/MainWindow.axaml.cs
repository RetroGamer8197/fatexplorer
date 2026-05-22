using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;

namespace fatimageexplorer;

public partial class MainWindow : Window
{
    List<string> listOfFiles = [];
    List<string> listOfDirs = [];
    List<string> listOfPartitions = [];
    int selected = 0;
    Image image;
    ListBox filesListBox;
    ListBox directoriesListBox;
    Label previewLabel, directoriesLabel, filesLabel, partitionLabel;
    TextBox preview;
    ComboBox partitionSelection;

    Button EnterSubdir, ParentDir;

    public MainWindow()
    {
        InitializeComponent();

        Width = 800;
        Height=450;
        Background = Brushes.Black;

        Button extractButton = new Button() {Content = "Extract", Width = 100, Background=Brushes.DarkSlateGray, Foreground=Brushes.White};
        extractButton.Click += Extract;

        MainCanvas.Children.Add(extractButton);
        Canvas.SetLeft(extractButton, 140);
        Canvas.SetBottom(extractButton, 20);

        Button exportButton = new Button() {Content = "Export", Width = 100, Background=Brushes.DarkSlateGray, Foreground=Brushes.White};
        exportButton.Click += Export;

        MainCanvas.Children.Add(exportButton);
        Canvas.SetLeft(exportButton, 380);
        Canvas.SetBottom(exportButton, 20);

        Button injectButton = new Button() {Content = "Inject", Width = 100, Background=Brushes.DarkSlateGray, Foreground=Brushes.White, };
        injectButton.Click += Inject;

        MainCanvas.Children.Add(injectButton);
        Canvas.SetLeft(injectButton, 260);
        Canvas.SetBottom(injectButton, 20);

        Button openButton = new Button() { Content = "Open", Width = 100, Background=Brushes.DarkSlateGray, Foreground=Brushes.White};
        openButton.Click += Open;

        filesListBox = new ListBox() {ItemsSource = listOfFiles, Height = 180, Width = 500, Background=Brushes.DarkSlateGray, Foreground=Brushes.White, FontFamily = new("monospace")};
        filesListBox.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
        filesListBox.SelectionChanged += FileSelectionChanged;
        MainCanvas.Children.Add(filesListBox);
        Canvas.SetLeft(filesListBox, 20);
        Canvas.SetTop(filesListBox, 180);

        filesLabel = new() {Content = "Files:", Foreground=Brushes.White};
        MainCanvas.Children.Add(filesLabel);
        Canvas.SetLeft(filesLabel, 20);
        Canvas.SetTop(filesLabel, 160);

        directoriesListBox = new ListBox() {ItemsSource = listOfDirs, Height = 100, Width = 400, Background=Brushes.DarkSlateGray, Foreground=Brushes.White, FontFamily = new("monospace")};
        directoriesListBox.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
        //directoriesListBox.SelectionChanged += FileSelectionChanged;
        MainCanvas.Children.Add(directoriesListBox);
        Canvas.SetLeft(directoriesListBox, 20);
        Canvas.SetTop(directoriesListBox, 40);

        directoriesLabel = new() {Content = "Directories:", Foreground=Brushes.White};
        MainCanvas.Children.Add(directoriesLabel);
        Canvas.SetLeft(directoriesLabel, 20);
        Canvas.SetTop(directoriesLabel, 20);

        ParentDir = new() {Content = ".. (^)", Width=80, Background=Brushes.DarkSlateGray, Foreground=Brushes.White};
        ParentDir.Click+= Parent_Click;
        MainCanvas.Children.Add(ParentDir);
        Canvas.SetLeft(ParentDir, 440);
        Canvas.SetTop(ParentDir, 40);

        EnterSubdir = new() {Content = "Enter", Width=80, Background=Brushes.DarkSlateGray, Foreground=Brushes.White};
        EnterSubdir.Click+= EnterSubdir_Click;
        MainCanvas.Children.Add(EnterSubdir);
        Canvas.SetLeft(EnterSubdir, 440);
        Canvas.SetTop(EnterSubdir, 110);
        
        MainCanvas.Children.Add(openButton);
        Canvas.SetLeft(openButton, 20);
        Canvas.SetBottom(openButton, 20);

        Button deleteButton = new Button() {Content = "Delete", Width = 100, Background=Brushes.DarkSlateGray, Foreground=Brushes.White};
        deleteButton.Click += Delete;

        MainCanvas.Children.Add(deleteButton);
        Canvas.SetLeft(deleteButton, 500);
        Canvas.SetBottom(deleteButton, 20);

        partitionSelection = new() {ItemsSource = listOfPartitions, Width = 240, Background=Brushes.DarkSlateGray, Foreground=Brushes.White};
        partitionSelection.SelectionChanged += SelectPartition;

        MainCanvas.Children.Add(partitionSelection);
        Canvas.SetLeft(partitionSelection, 540);
        Canvas.SetTop(partitionSelection, 40);

        partitionLabel = new() {Content = "Partition:", Foreground=Brushes.White};
        MainCanvas.Children.Add(partitionLabel);
        Canvas.SetLeft(partitionLabel, 540);
        Canvas.SetTop(partitionLabel, 20);

        preview = new() {Text = "", Width = 240, Height = 260, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, AcceptsTab = true, Background=Brushes.DarkSlateGray, Foreground=Brushes.White};

        MainCanvas.Children.Add(preview);
        Canvas.SetLeft(preview, 540);
        Canvas.SetTop(preview, 100);

        previewLabel = new() {Content = "Preview:", Width = 240, Foreground=Brushes.White};

        MainCanvas.Children.Add(previewLabel);
        Canvas.SetLeft(previewLabel, 540);
        Canvas.SetTop(previewLabel, 80);
        
    }

    public async void Extract(object sender, RoutedEventArgs routedEventArgs)
    {
        Console.WriteLine("Extract!");

        if (filesListBox.SelectedItem == null)
        {
            Console.WriteLine("No image opened or no item selected");
            Window popup = new Popup("No image opened or no item selected");
            popup.ShowDialog(this);
            return;
        }

        var topLevel = GetTopLevel(this); 

        var filePath = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions {Title = "Extract file"});

        try
        {
            Console.WriteLine(filePath.TryGetLocalPath());
        } catch (Exception)
        {
            Console.WriteLine("Cancelled by user or file not found");
            Window popup = new Popup("Cancelled by user or file not found");
            popup.ShowDialog(this);
            return;
        }

        image.partitions[selected].ExtractFile((string)filePath.TryGetLocalPath(), image.partitions[selected].GetDirectoryEntryByName((string)filesListBox.SelectedItem), false);
    }

    public async void Delete(object sender, RoutedEventArgs routedEventArgs)
    {
        Console.WriteLine("Delete!");

        image.partitions[selected].Delete((string)filesListBox.SelectedItem);

        image = Image.OpenImage("exported.bin");
        if (image == null)
        {
            Console.WriteLine("Not a valid image!");
            Window popup = new Popup("Not a valid image!");
            popup.ShowDialog(this);
            return;
        }

        listOfFiles = [];
        foreach (DirectoryEntry entry in image.partitions[selected].rootDirectory)
        {
            listOfFiles.Add(entry.FileName + "." + entry.FileExtension);
        }
        filesListBox.ItemsSource = listOfFiles;

        listOfDirs = [];
        foreach (DirectoryEntry entry in image.partitions[selected].rootDirectorySubdirectories)
        {
            listOfDirs.Add(entry.FileName + entry.FileExtension);
        }
        directoriesListBox.ItemsSource = listOfDirs;
    }

    public async void Export(object sender, RoutedEventArgs routedEventArgs)
    {
        Console.WriteLine("Export!");

        var topLevel = GetTopLevel(this); 

        var filePath = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions {Title = "Extract file"});

        try
        {
            Console.WriteLine(filePath.TryGetLocalPath());
        } catch (Exception)
        {
            Console.WriteLine("Cancelled by user or cannot create file");
            Window popup = new Popup("Cancelled by user or cannot create file");
            popup.ShowDialog(this);
            return;
        }

        image.partitions[selected].Export(filePath.TryGetLocalPath());
    }

    public async void Inject(object sender, RoutedEventArgs routedEventArgs)
    {
        Console.WriteLine("Inject!");

        var topLevel = GetTopLevel(this); 

        var filePath = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {Title = "Select a file to inject", AllowMultiple = false});

        try
        {
            Console.WriteLine(filePath[0].TryGetLocalPath());
        } catch (Exception)
        {
            Console.WriteLine("Cancelled by user or file not found");
            Window popup = new Popup("Cancelled by user or file not found");
            popup.ShowDialog(this);
            return;
        }

        string fileName = filePath[0].TryGetLocalPath();

        string[] filenameParts = fileName.Split('/', '\\');

        string newFilename;

        if (filenameParts[filenameParts.Length - 1].Split('.')[0].Length <= 8)
        {
            newFilename = filenameParts[filenameParts.Length - 1].Split('.')[0];
        } else
        {
            newFilename = filenameParts[filenameParts.Length - 1].Split('.')[0].Substring(0, 6) + "~1";
        }
        string newExtension = "   ";
        if (filenameParts[filenameParts.Length - 1].Split('.').Length > 1)
        {
            if (filenameParts[filenameParts.Length - 1].Split('.')[1].Length > 3)
            {
                newExtension = filenameParts[filenameParts.Length - 1].Split('.')[1][..3];
            } else
            {
                newExtension = filenameParts[filenameParts.Length - 1].Split('.')[1];
            } 
        }

        newFilename = newFilename.ToUpper();
        newExtension = newExtension.ToUpper();

        image.partitions[selected].InjectFile(fileName, newFilename, newExtension);

        image = Image.OpenImage("exported.bin");
        if (image == null)
        {
            Console.WriteLine("Not a valid image!");
            Window popup = new Popup("Not a valid image!");
            popup.ShowDialog(this);
            return;
        }

        listOfFiles = [];
        foreach (DirectoryEntry entry in image.partitions[selected].currentDirectory)
        {
            listOfFiles.Add(entry.FileName + "." + entry.FileExtension);
        }
        filesListBox.ItemsSource = listOfFiles;

        listOfDirs = [];
        foreach (DirectoryEntry entry in image.partitions[selected].currentDirectorySubdirectories)
        {
            listOfDirs.Add(entry.FileName + entry.FileExtension);
        }
        directoriesListBox.ItemsSource = listOfDirs;
    }

    public async void Open(object sender, RoutedEventArgs routedEventArgs)
    {
        Console.WriteLine("Open!");

        var topLevel = GetTopLevel(this); 

        var filePath = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {Title = "Open an Image", AllowMultiple = false});
        try
        {
            Console.WriteLine(filePath[0].TryGetLocalPath());
        } catch (Exception)
        {
            Console.WriteLine("Cancelled by user or file not found");
            Window popup = new Popup("Cancelled by user or file not found");
            popup.ShowDialog(this);
            return;
        }

        image = Image.OpenImage(filePath[0].TryGetLocalPath());
        if (image == null)
        {
            Console.WriteLine("Not a valid image!");
            Window popup = new Popup("Not a valid image!");
            popup.ShowDialog(this);
            return;
        }

        listOfPartitions = [];
        foreach (Partition partition in image.partitions)
        {
            if (partition.GetName() != "")
            {
                listOfPartitions.Add(partition.index + ": " + partition.GetName());
            }
        }
        partitionSelection.ItemsSource = listOfPartitions;
        partitionSelection.SelectedIndex = 0;
        selected = partitionSelection.SelectedIndex;

        if (image.partitions.Count > 0)
        {
        } else
        {
            Console.WriteLine("No partitions in image!");
            Window popup = new Popup("No partitions in image!");
            popup.ShowDialog(this);
            return;
        }
    }

    public async void SelectPartition(object sender, RoutedEventArgs routedEventArgs)
    {
        if (partitionSelection.SelectedIndex == -1)
        {
            return;
        }
        selected = partitionSelection.SelectedIndex;

        listOfFiles = [];
        foreach (DirectoryEntry entry in image.partitions[selected].currentDirectory)
        {
            listOfFiles.Add(entry.FileName + "." + entry.FileExtension);
        }
        filesListBox.ItemsSource = listOfFiles;

        listOfDirs = [];
        foreach (DirectoryEntry entry in image.partitions[selected].currentDirectorySubdirectories)
        {
            listOfDirs.Add(entry.FileName + entry.FileExtension);
        }
        directoriesListBox.ItemsSource = listOfDirs;
    }

    public void FileSelectionChanged(object sender, RoutedEventArgs routedEventArgs)
    {
        string selectedItem;
        if (filesListBox.SelectedIndex > listOfFiles.Count || filesListBox.SelectedIndex == -1)
        {
            preview.Text = "";
            return;
        }
        try
        {
           selectedItem = filesListBox.SelectedItem.ToString();
        } catch (Exception)
        {
            preview.Text = "";
            return;
        }
        if (selectedItem.Split('.')[1] == "TXT" || selectedItem.Split('.')[1] == "BAT" || selectedItem.Split('.')[1] == "MD")
        {
            image.partitions[selected].ExtractFile("temp.txt", image.partitions[selected].GetDirectoryEntryByName(selectedItem), false);
            StreamReader streamReader = new(File.Open("temp.txt", FileMode.OpenOrCreate));
            preview.Text = streamReader.ReadToEnd();
            streamReader.Close();
        } else
        {
            preview.Text = "";
        }
    }

    public void EnterSubdir_Click(object sender, RoutedEventArgs routedEventArgs)
    {
        if (directoriesListBox.SelectedIndex != -1)
        {
            DirectoryEntry tempEntry = image.partitions[selected].GetDirectoryEntryByName(directoriesListBox.SelectedItem.ToString());

            image.partitions[selected].OpenSubdir(tempEntry);

            UpdateItemListsToCurrentDirectory();
        }
    }

    public void Parent_Click(object sender, RoutedEventArgs routedEventArgs)
    {
        if (image.partitions[selected].ParentDirs.Count == 0)
        {
            return;
        } 
        else
        {
            image.partitions[selected].PopulateSubdir(image.partitions[selected].ParentDirs.Pop());
        }

        UpdateItemListsToCurrentDirectory();
    }

    public void UpdateItemListsToCurrentDirectory()
    {
        listOfFiles = [];
            foreach (DirectoryEntry entry in image.partitions[selected].currentDirectory)
            {
                listOfFiles.Add(entry.FileName + "." + entry.FileExtension);
            }
            filesListBox.ItemsSource = listOfFiles;

            listOfDirs = [];
            foreach (DirectoryEntry entry in image.partitions[selected].currentDirectorySubdirectories)
            {
                listOfDirs.Add(entry.FileName + entry.FileExtension);
            }
            directoriesListBox.ItemsSource = listOfDirs;
    }
}