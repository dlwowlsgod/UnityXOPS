#include "pch.h"
#include "assimp_import.h"

MeshData* ImportModel(const char* filePath, ImportProfile profile) {
    unsigned int assimpFlags = aiProcess_Triangulate | aiProcess_MakeLeftHanded | aiProcess_FlipWindingOrder;
    const aiScene* scene = nullptr;
    Assimp::Importer importer;

    switch (profile) {
        //force abort
        case IMPORT_ABORT:
            return nullptr;

        //normal xfile import
        case IMPORT_XFILE:
            scene = importer.ReadFile(filePath, assimpFlags);
            break;

        //xfile import if fixtoken enable
        case IMPORT_XFILE_FIXTOKEN: {
            std::ifstream file(filePath, std::ios::binary);
            if (!file.is_open()) {
                return nullptr;
            }

            char magic[16];
            file.read(magic, 16);
            file.seekg(0, std::ios::beg);

            // Check for "xof 0302txt 0032" or similar text format header
            if (magic[8] == 't' && magic[9] == 'x' && magic[10] == 't') {
                std::stringstream ss;
                
                // Write header
                ss.write(magic, 16);
                
                // Add a newline character to ensure token separation
                ss.put('\n');

                // Write the rest of the file
                ss << file.rdbuf();
                
                std::string content = ss.str();
                scene = importer.ReadFileFromMemory(content.c_str(), content.length(), assimpFlags);
            } else {
                // If it's binary, treat it like a normal XFile import
                scene = importer.ReadFile(filePath, assimpFlags);
            }
            break;
        }

        default:
            return nullptr;
    }

    if (!scene || !scene->HasMeshes()) {
        return nullptr;
    }

    //calculate total mesh data
    unsigned int totalVertices = 0;
    unsigned int totalIndices = 0;
    for (unsigned int i = 0; i < scene->mNumMeshes; ++i) {
        totalVertices += scene->mMeshes[i]->mNumVertices;
        totalIndices += scene->mMeshes[i]->mNumFaces * 3;
    }

    if (totalVertices == 0) {
        return nullptr;
    }

    //allocate data blocks
    size_t totalSize = sizeof(MeshData) + sizeof(Vector3) * totalVertices + sizeof(Vector2) * totalVertices + sizeof(int) * totalIndices;

    char* buffer = new char[totalSize];
    MeshData* meshData = reinterpret_cast<MeshData*>(buffer);

    //set pointer
    meshData->vertices = reinterpret_cast<Vector3*>(buffer + sizeof(MeshData));
    meshData->uvs = reinterpret_cast<Vector2*>(reinterpret_cast<char*>(meshData->vertices) + sizeof(Vector3) * totalVertices);
    meshData->indices = reinterpret_cast<int*>(reinterpret_cast<char*>(meshData->uvs) + sizeof(Vector2) *totalVertices);

    //set counts
    meshData->vertexCount = totalVertices;
    meshData->uvCount = totalVertices;
    meshData->indexCount = totalIndices;

    //copy data and combine
    unsigned int vertexOffset = 0;
    unsigned int indexOffset = 0;

    for (unsigned int i = 0; i < scene->mNumMeshes; ++i) {
        aiMesh* mesh = scene->mMeshes[i];

        //copy vertices
        for (unsigned int j = 0; j < mesh->mNumVertices; ++j) {
            meshData->vertices[vertexOffset + j].x = mesh->mVertices[j].x;
            meshData->vertices[vertexOffset + j].y = mesh->mVertices[j].y;
            //meshData->vertices[vertexOffset + j].z = -mesh->mVertices[j].z;
            meshData->vertices[vertexOffset + j].z = mesh->mVertices[j].z;
        }

        //copy uv
        if (mesh->HasTextureCoords(0)) {
            for (unsigned int j = 0; j < mesh->mNumVertices; ++j) {
                meshData->uvs[vertexOffset + j].x = mesh->mTextureCoords[0][j].x;
                meshData->uvs[vertexOffset + j].y = mesh->mTextureCoords[0][j].y;
            }
        }

        //copy indices
        for (unsigned int j = 0; j < mesh->mNumFaces; ++j) {
            aiFace face = mesh->mFaces[j];
            meshData->indices[indexOffset + j * 3 + 0] = face.mIndices[0] + vertexOffset;
            //meshData->indices[indexOffset + j * 3 + 1] = face.mIndices[2] + vertexOffset;
            //meshData->indices[indexOffset + j * 3 + 2] = face.mIndices[1] + vertexOffset;
            meshData->indices[indexOffset + j * 3 + 1] = face.mIndices[1] + vertexOffset;
            meshData->indices[indexOffset + j * 3 + 2] = face.mIndices[2] + vertexOffset;
        }

        vertexOffset += mesh->mNumVertices;
        indexOffset += mesh->mNumFaces * 3;
    }

    return meshData;
}

void FreeModel(MeshData* data) {
    if (data) {
        delete[] reinterpret_cast<char*>(data);
    }
}

void GetAssimpVersion(unsigned int* major, unsigned int* minor, unsigned int* patch, unsigned int* revision) {
    if (major) {
        *major = aiGetVersionMajor();
    }
    if (minor) {
        *minor = aiGetVersionMinor();
    }
    if (patch) {
        *patch = aiGetVersionPatch();
    }
    if (revision) {
        *revision = aiGetVersionRevision();
    }
}