using MemorialFloor.Domain;
using UnityEngine;

namespace MemorialFloor.Game
{
    /// <summary>場所の出口。行き先は出口の種類と、その時の受注で決まる</summary>
    public sealed class Exit : MonoBehaviour
    {
        [SerializeField] private ExitKind kind;

        public ExitKind Kind
        {
            get { return kind; }
        }
    }
}
