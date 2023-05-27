using System.Text;

using Azure;
using Azure.AI.Vision.Common.Input;
using Azure.AI.Vision.Common.Options;
using Azure.AI.Vision.ImageAnalysis;

namespace VisionApp;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
	}

	string photoPath = string.Empty;

	private async void SelectPictureButton_Clicked(object sender, EventArgs e)
	{
		FileResult photo = await MediaPicker.Default.PickPhotoAsync();

		if (photo != null)
		{
			// save the file into local storage
			string localFilePath = System.IO.Path.Combine(FileSystem.AppDataDirectory, photo.FileName);

			using Stream sourceStream = await photo.OpenReadAsync();
			using FileStream localFileStream = File.OpenWrite(localFilePath);

			await sourceStream.CopyToAsync(localFileStream);
			sourceStream.Close();

			SelectedImage.Source = ImageSource.FromFile(photo.FullPath);
			photoPath = photo.FullPath;
		}
	}

	private async void AnalyzeButton_Clicked(object sender, EventArgs e)
	{
		await AnalyzeImage(photoPath);
	}

	async Task AnalyzeImage(string imagePath)
	{
		var serviceOptions = new VisionServiceOptions(
			"https://cog-ms-learn-vision-labp.cognitiveservices.azure.com/",
			new AzureKeyCredential(""));

		using var imageSource = VisionSource.FromFile(imagePath);

		var analysisOptions = new ImageAnalysisOptions()
		{
			Features = ImageAnalysisFeature.Caption 
				| ImageAnalysisFeature.DenseCaptions 
				| ImageAnalysisFeature.Text
				| ImageAnalysisFeature.Tags
				| ImageAnalysisFeature.Objects
				| ImageAnalysisFeature.People,

			Language = "en",
			GenderNeutralCaption = true
		};

		using var analyzer = new ImageAnalyzer(serviceOptions, imageSource, analysisOptions);

		var result = await analyzer.AnalyzeAsync();
		var text = new StringBuilder();

		if (result.Reason == ImageAnalysisResultReason.Analyzed)
		{
			if (result.Caption != null)
			{
				text.AppendLine("----------- CAPTION --------------");
				text.AppendLine($"\"{result.Caption.Content}\", Confidence {result.Caption.Confidence:0.0000}");
				text.AppendLine("----------------------------------------");
			}

			if (result.DenseCaptions != null)
			{
				text.AppendLine("----------- DENSE CAPTIONS --------------");

				foreach (var caption in result.DenseCaptions)
				{
					string pointsToString = "{" + caption.BoundingBox.ToString() + "}";
					text.AppendLine($"'{caption.Content}', Confidence {caption.Confidence:0.0000}, Bounding box {pointsToString}");
				}

				text.AppendLine("----------------------------------------");
			}

			if (result.Text != null)
			{
				text.AppendLine("----------- TEXT --------------");

				foreach (var line in result.Text.Lines)
				{
					string pointsToString = "{" + string.Join(',', line.BoundingPolygon.Select(pointsToString => pointsToString.ToString())) + "}";
					text.AppendLine($"'{line.Content}', Bounding polygon {pointsToString}");
				}

				text.AppendLine("----------------------------------------");
			}

			if (result.People != null)
			{
				text.AppendLine("----------- PEOPLE --------------");

				foreach (var people in result.People)
				{
					string pointsToString = "{" + people.BoundingBox.ToString() + "}";
					text.AppendLine($"'Confidence {people.Confidence:0.0000}, Bounding box {pointsToString}");
				}

				text.AppendLine("----------------------------------------");
			}

			if (result.People != null)
			{
				text.AppendLine("----------- OBJECTS --------------");

				foreach (var obj in result.Objects)
				{
					string pointsToString = "{" + obj.BoundingBox.ToString() + "}";
					text.AppendLine($"'{obj.Name}', 'Confidence {obj.Confidence:0.0000}, Bounding box {pointsToString}");
				}

				text.AppendLine("----------------------------------------");
			}

			if (result.Tags != null)
			{
				text.AppendLine("----------- TAGS --------------");

				foreach (var tag in result.Tags)
				{
					text.AppendLine($"'{tag.Name}', 'Confidence {tag.Confidence:0.0000}");
				}

				text.AppendLine("----------------------------------------");
			}
		}
		else
		{
			var errorDetails = ImageAnalysisErrorDetails.FromResult(result);
			text.AppendLine(" Analysis failed.");
			text.AppendLine($" Error reason: {errorDetails.Reason}");
			text.AppendLine($" Error code: {errorDetails.ErrorCode}");
			text.AppendLine($" Error message: {errorDetails.Message}");
		}

		EditorResult.Text = text.ToString();
	}
}

