using System;
using System.IO;
using System.Diagnostics.CodeAnalysis;

namespace JJLUtility
{
    public static class SafePath
    {
        // Windows는 대소문자 무시, Unix 계열은 구분
        private static readonly StringComparison PathComparison =
            Path.DirectorySeparatorChar == '\\'
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

        public static string Combine([NotNull] string root, [NotNull] params string[] paths)
        {
            // root를 정규화하고, 끝에 구분자를 붙여 접두사 비교를 정확하게
            string fullRoot = Path.GetFullPath(root);
            if (!fullRoot.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                fullRoot += Path.DirectorySeparatorChar;

            // root + paths 를 순서대로 합침
            string[] allParts = new string[paths.Length + 1];
            allParts[0] = root;
            Array.Copy(paths, 0, allParts, 1, paths.Length);

            string combined = Path.Combine(allParts);
            string fullCombined = Path.GetFullPath(combined);

            // fullCombined가 fullRoot로 시작하지 않으면 탈출 시도로 간주
            if (!fullCombined.StartsWith(fullRoot, PathComparison))
            {
                Debugger.LogError($"Traversal directory detected: '{string.Join(", ", paths)}' → '{fullCombined}' is outside of '{root}'");
                return null;
            }

            return fullCombined;
        }
    }
}
