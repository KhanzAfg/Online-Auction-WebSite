# Create sample-images directory if it doesn't exist
$sampleImagesPath = Join-Path $PSScriptRoot "..\wwwroot\sample-images"
if (-not (Test-Path $sampleImagesPath)) {
    New-Item -ItemType Directory -Path $sampleImagesPath -Force
}

# Categories and their image counts
$categories = @(
    "vehicles",
    "electronics",
    "gaming",
    "fashion",
    "home & garden",
    "sports",
    "collectibles",
    "books",
    "jewelry",
    "art"
)

# Download images for each category
foreach ($category in $categories) {
    Write-Host "Downloading images for $category..."
    
    # Create 5 images for each category
    for ($i = 1; $i -le 5; $i++) {
        $fileName = "$($category)_$i.jpg"
        $filePath = Join-Path $sampleImagesPath $fileName
        
        # Use Picsum Photos for random images
        $imageUrl = "https://picsum.photos/800/600"
        
        try {
            # Download the image
            Invoke-WebRequest -Uri $imageUrl -OutFile $filePath
            Write-Host "Downloaded $fileName"
            
            # Add a small delay to avoid rate limiting
            Start-Sleep -Milliseconds 500
        }
        catch {
            Write-Host "Error downloading $fileName : $_"
        }
    }
}

Write-Host "`nImage download complete!"
Write-Host "Images have been saved to: $sampleImagesPath" 