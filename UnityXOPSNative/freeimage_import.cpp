#include "pch.h"
#include "freeimage_import.h"

struct FreeImageDeleter {
	void operator()(FIBITMAP* dib) const {
		if (dib) {
			FreeImage_Unload(dib);
		}
	}
};

ImageData* ImportImage(const char* filePath, ImageImportProfile profile) {
	FREE_IMAGE_FORMAT fif = FIF_UNKNOWN;
	
	switch (profile) {
	case IMPORT_ABORT:
		return nullptr;
	case IMPORT_JPG:
		fif = FIF_JPEG;
		break;
	case IMPORT_PNG:
		fif = FIF_PNG;
		break;
	case IMPORT_BMP:
		fif = FIF_BMP;
		break;
	case IMPORT_TGA:
		fif = FIF_TARGA;
		break;
	case IMPORT_DDS:
		fif = FIF_DDS;
		break;
	}

	if (fif == FIF_UNKNOWN) {
		fif = FreeImage_GetFileType(filePath, 0);
	}

	if (fif == FIF_UNKNOWN || !FreeImage_FIFSupportsReading(fif)) {
		return nullptr;
	}

	std::unique_ptr<FIBITMAP, FreeImageDeleter> dib(FreeImage_Load(fif, filePath, 0));
	if (!dib) {
		return nullptr;
	}

	std::unique_ptr<FIBITMAP, FreeImageDeleter> dib32(FreeImage_ConvertTo32Bits(dib.get()));
	if (!dib32) {
		return nullptr;
	}

	BYTE* bits = FreeImage_GetBits(dib32.get());
	int width = FreeImage_GetWidth(dib32.get());
	int height = FreeImage_GetHeight(dib32.get());
	if (!bits || width == 0 || height == 0) {
		return nullptr;
	}

	size_t pixelDataSize = width * height * 4;
	size_t totalSize = sizeof(ImageData) + pixelDataSize;
	char* buffer = new char[totalSize];

	ImageData* imageData = reinterpret_cast<ImageData*>(buffer);
	imageData->width = width;
	imageData->height = height;
	imageData->pixelData = reinterpret_cast<unsigned char*>(buffer + sizeof(ImageData));

	for (int i = 0; i < width * height; ++i) {
		BYTE r = bits[i * 4 + 0];
		BYTE g = bits[i * 4 + 1];
		BYTE b = bits[i * 4 + 2];
		BYTE a = bits[i * 4 + 3];

		imageData->pixelData[i * 4 + 0] = r;
		imageData->pixelData[i * 4 + 1] = g;
		imageData->pixelData[i * 4 + 2] = b;
		imageData->pixelData[i * 4 + 3] = a;
	}


	return imageData;
}

void DeAllocImage(ImageData* data) {
	if (data) {
		delete[] reinterpret_cast<char*>(data);
	}
}

const char* GetFreeImageVersion() {
	return FreeImage_GetVersion();
}