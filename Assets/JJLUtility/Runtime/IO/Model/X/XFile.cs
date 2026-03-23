using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Globalization;

namespace JJLUtility.IO
{
    /// <summary>
    /// 파싱된 .x 파일의 모든 메시 데이터를 담는 컨테이너 클래스.
    /// </summary>
    public class XFile
    {
        public List<XMeshData> Meshes = new List<XMeshData>();
    }

    public partial class ModelLoader
    {
        /// <summary>
        /// .x 파일 텍스트를 토큰 단위로 파싱하는 내부 클래스.
        /// </summary>
        private class XTokenizer
        {
            private readonly string _text;
            private int _pos;

            /// <summary>
            /// 지정된 텍스트로 토크나이저를 초기화한다.
            /// </summary>
            public XTokenizer(string text)
            {
                _text = text;
                _pos = 0;
            }

            /// <summary>
            /// 공백, 구분자, 주석을 건너뛰어 다음 토큰의 시작 위치로 이동한다.
            /// </summary>
            private void SkipSeparators()
            {
                while (_pos < _text.Length)
                {
                    char c = _text[_pos];
                    if (c == ' ' || c == '\t' || c == '\r' || c == '\n' || c == ';' || c == ',')
                    {
                        _pos++;
                    }
                    else if (c == '/' && _pos + 1 < _text.Length && _text[_pos + 1] == '/')
                    {
                        while (_pos < _text.Length && _text[_pos] != '\n') _pos++;
                    }
                    else if (c == '#')
                    {
                        while (_pos < _text.Length && _text[_pos] != '\n') _pos++;
                    }
                    else break;
                }
            }

            /// <summary>
            /// 현재 위치를 변경하지 않고 다음 토큰을 미리 읽어 반환한다.
            /// </summary>
            public string Peek()
            {
                int saved = _pos;
                string token = Read();
                _pos = saved;
                return token;
            }

            /// <summary>
            /// 현재 위치에서 다음 토큰을 읽고 위치를 전진시켜 반환한다.
            /// </summary>
            public string Read()
            {
                SkipSeparators();
                if (_pos >= _text.Length) return null;

                char c = _text[_pos];

                if (c == '{') { _pos++; return "{"; }
                if (c == '}') { _pos++; return "}"; }

                // GUID 건너뜀: <xxxxxxxx-...>
                if (c == '<')
                {
                    while (_pos < _text.Length && _text[_pos] != '>') _pos++;
                    if (_pos < _text.Length) _pos++;
                    return Read();
                }

                // 문자열 리터럴
                if (c == '"')
                {
                    _pos++;
                    int start = _pos;
                    while (_pos < _text.Length && _text[_pos] != '"') _pos++;
                    string str = _text.Substring(start, _pos - start);
                    if (_pos < _text.Length) _pos++;
                    return "\"" + str + "\"";
                }

                // 식별자 또는 숫자
                {
                    int start = _pos;
                    while (_pos < _text.Length)
                    {
                        char ch = _text[_pos];
                        if (ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n' ||
                            ch == ';' || ch == ',' || ch == '{' || ch == '}' ||
                            ch == '<' || ch == '>')
                            break;
                        _pos++;
                    }
                    return _text.Substring(start, _pos - start);
                }
            }

            /// <summary>
            /// 다음 토큰을 float으로 파싱해 반환한다.
            /// </summary>
            public float ReadFloat()
            {
                return float.Parse(Read(), CultureInfo.InvariantCulture);
            }

            /// <summary>
            /// 다음 토큰을 int로 파싱해 반환한다.
            /// </summary>
            public int ReadInt()
            {
                return int.Parse(Read(), CultureInfo.InvariantCulture);
            }

            // '{' 를 이미 소비한 상태에서 호출 - matching '}' 까지 전부 건너뜀
            /// <summary>
            /// 현재 블록의 닫는 중괄호까지 모든 토큰을 건너뛴다. '{' 는 이미 소비한 상태여야 한다.
            /// </summary>
            public void SkipBlock()
            {
                int depth = 1;
                while (_pos < _text.Length && depth > 0)
                {
                    string t = Read();
                    if (t == null) break;
                    if (t == "{") depth++;
                    else if (t == "}") depth--;
                }
            }
        }

        // 블록 이름이 있을 수도 없을 수도 있음 (예: "Mesh {" vs "Mesh obj11 {")
        /// <summary>
        /// 블록 앞에 선택적으로 붙는 이름 토큰이 있으면 읽어 건너뛴다.
        /// </summary>
        private static void SkipOptionalName(XTokenizer tokenizer)
        {
            if (tokenizer.Peek() != "{")
                tokenizer.Read();
        }

        /// <summary>
        /// 지정 경로의 .x 파일을 파싱해 XFile 객체로 반환한다.
        /// </summary>
        /// <param name="filepath">.x 파일 경로.</param>
        /// <returns>파싱된 XFile. 실패 시 null.</returns>
        private static XFile LoadXFile(string filepath)
        {
            if (!File.Exists(filepath))
            {
                Debugger.LogError($"File not found: {filepath}", null, nameof(ModelLoader));
                return null;
            }

            string text = File.ReadAllText(filepath, System.Text.Encoding.UTF8);

            if (text.Length < 16 || !text.StartsWith("xof"))
            {
                Debugger.LogError($"File is not a valid .x file: {filepath}", null, nameof(ModelLoader));
                return null;
            }

            // 헤더는 정확히 16바이트: "xof " + version(4) + format(3) + " " + floatsize(4)
            // 가끔 있는 줄바꿈 없는 헤더도 고려하여 16바이트 이후부터 파싱
            string body = text.Substring(16);

            try
            {
                var tokenizer = new XTokenizer(body);
                var xFile = new XFile();
                ParseTopLevel(tokenizer, xFile);
                return xFile;
            }
            catch (System.Exception e)
            {
                Debugger.LogError($"Failed to parse .x file: {filepath}\n{e.Message}", null, nameof(ModelLoader));
                return null;
            }
        }

        /// <summary>
        /// .x 파일 최상위 블록을 순회하며 Frame과 Mesh를 파싱한다.
        /// </summary>
        private static void ParseTopLevel(XTokenizer tokenizer, XFile xFile)
        {
            string token;
            while ((token = tokenizer.Read()) != null)
            {
                switch (token)
                {
                    case "template":
                        tokenizer.Read(); // 이름
                        tokenizer.Read(); // {
                        tokenizer.SkipBlock();
                        break;

                    case "Header":
                        SkipOptionalName(tokenizer);
                        tokenizer.Read(); // {
                        tokenizer.SkipBlock();
                        break;

                    case "Frame":
                        SkipOptionalName(tokenizer);
                        tokenizer.Read(); // {
                        ParseFrame(tokenizer, xFile);
                        break;

                    case "Mesh":
                        SkipOptionalName(tokenizer);
                        tokenizer.Read(); // {
                        var meshData = ParseMesh(tokenizer);
                        if (meshData != null) xFile.Meshes.Add(meshData);
                        break;

                    default:
                        if (tokenizer.Peek() == "{")
                        {
                            tokenizer.Read(); // {
                            tokenizer.SkipBlock();
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Frame 블록을 재귀적으로 파싱해 내부 Mesh를 XFile에 추가한다.
        /// </summary>
        private static void ParseFrame(XTokenizer tokenizer, XFile xFile)
        {
            string token;
            while ((token = tokenizer.Peek()) != null && token != "}")
            {
                token = tokenizer.Read();
                switch (token)
                {
                    case "FrameTransformMatrix":
                        SkipOptionalName(tokenizer);
                        tokenizer.Read(); // {
                        tokenizer.SkipBlock();
                        break;

                    case "Frame":
                        SkipOptionalName(tokenizer);
                        tokenizer.Read(); // {
                        ParseFrame(tokenizer, xFile); // 중첩 Frame 재귀
                        break;

                    case "Mesh":
                        SkipOptionalName(tokenizer);
                        tokenizer.Read(); // {
                        var meshData = ParseMesh(tokenizer);
                        if (meshData != null) xFile.Meshes.Add(meshData);
                        break;

                    default:
                        if (tokenizer.Peek() == "{")
                        {
                            tokenizer.Read(); // {
                            tokenizer.SkipBlock();
                        }
                        break;
                }
            }

            if (tokenizer.Peek() == "}") tokenizer.Read();
        }

        /// <summary>
        /// Mesh 블록을 파싱해 정점, 면, UV를 포함하는 XMeshData를 반환한다.
        /// </summary>
        private static XMeshData ParseMesh(XTokenizer tokenizer)
        {
            var meshData = new XMeshData();

            // 정점
            int nVertices = tokenizer.ReadInt();
            for (int i = 0; i < nVertices; i++)
            {
                float x = tokenizer.ReadFloat();
                float y = tokenizer.ReadFloat();
                float z = tokenizer.ReadFloat();
                meshData.Vertices.Add(new Vector3(x, y, z));
                meshData.UVs.Add(Vector2.zero); // TextureCoords 파싱 전 기본값
            }

            // 면 - 쿼드/트라이 혼합, fan 삼각화
            // DirectX와 Unity 모두 왼손 좌표계 + CW = 앞면이므로 와인딩 반전 불필요
            int nFaces = tokenizer.ReadInt();
            for (int i = 0; i < nFaces; i++)
            {
                int nVerts = tokenizer.ReadInt();
                int[] idx = new int[nVerts];
                for (int j = 0; j < nVerts; j++)
                    idx[j] = tokenizer.ReadInt();

                for (int j = 1; j < nVerts - 1; j++)
                {
                    meshData.Indices.Add(idx[0]);
                    meshData.Indices.Add(idx[j]);
                    meshData.Indices.Add(idx[j + 1]);
                }
            }

            // 자식 블록 파싱
            string token;
            while ((token = tokenizer.Peek()) != null && token != "}")
            {
                token = tokenizer.Read();
                switch (token)
                {
                    case "MeshTextureCoords":
                        SkipOptionalName(tokenizer);
                        tokenizer.Read(); // {
                        ParseMeshTextureCoords(tokenizer, meshData);
                        break;

                    // MeshNormals, MeshMaterialList 등 전부 건너뜀
                    default:
                        if (tokenizer.Peek() == "{")
                        {
                            tokenizer.Read(); // {
                            tokenizer.SkipBlock();
                        }
                        break;
                }
            }

            if (tokenizer.Peek() == "}") tokenizer.Read();

            return meshData;
        }

        /// <summary>
        /// MeshTextureCoords 블록을 파싱해 meshData의 UV 목록을 갱신한다.
        /// </summary>
        private static void ParseMeshTextureCoords(XTokenizer tokenizer, XMeshData meshData)
        {
            int nCoords = tokenizer.ReadInt();
            int limit = Mathf.Min(nCoords, meshData.UVs.Count);

            for (int i = 0; i < nCoords; i++)
            {
                float u = tokenizer.ReadFloat();
                float v = tokenizer.ReadFloat();
                if (i < limit)
                    meshData.UVs[i] = new Vector2(u, 1f - v); // DirectX V=0 상단 -> Unity V=0 하단
            }

            if (tokenizer.Peek() == "}") tokenizer.Read();
        }
    }
}
