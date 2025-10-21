#pragma once

#include <assimp/Importer.hpp>
#include <assimp/scene.h>
#include <assimp/postprocess.h>
#include <assimp/version.h>

#include <fstream>
#include <vector>
#include <sstream>

//Need to keep consistency with C# class (StructLayout.Sequential)
struct Vector3 {
	float x, y, z;
};

struct Vector2 {
	float x, y;
};

struct MeshData {
	Vector3* vertices;
	int vertexCount;
	Vector2* uvs;
	int uvCount;
	int* indices;
	int indexCount;
};

enum ImportProfile : unsigned int {
	IMPORT_ABORT = 0,
	IMPORT_XFILE = 1,
	IMPORT_XFILE_FIXTOKEN = 2
};

extern "C" {
	__declspec(dllexport) MeshData* ImportModel(const char* filePath, ImportProfile profile);
	__declspec(dllexport) void FreeModel(MeshData* data);
	__declspec(dllexport) void GetAssimpVersion(unsigned int* major, unsigned int* minor, unsigned int* patch, unsigned int* revision);
}