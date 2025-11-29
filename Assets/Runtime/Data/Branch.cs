using UnityEngine;

namespace UnityXOPS
{
    public struct Branch
    {
        public int A;
        public int B;
        
        public Branch(int a, int b)
        {
            A = a;
            B = b;
        }

        public int Get()
        {
            return Random.Range(0, 2) == 0 ? A : B; 
        }
    }
}