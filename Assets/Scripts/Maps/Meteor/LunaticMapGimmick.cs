using UnityEngine;
using System.Collections.Generic;

public class LunaticMapGimmick : MonoBehaviour
{
    #region Variables_Exposed_in_Inspector
    [Header("메테오 애니메이션용\n임시로 게임 오브젝트")]
    [SerializeField] private GameObject _smallMeteor;
    [SerializeField] private GameObject _mediumMeteor;
    [SerializeField] private GameObject _largeMeteor;

    [SerializeField] private ParticleSystem _particle;
    #endregion

    #region Private_Variables



    #endregion


    ParticleSystem.Burst burst;

    private void Awake()
    {
        _particle.useAutoRandomSeed = false;
        //_particle1.useAutoRandomSeed = false;
        //_particle2.useAutoRandomSeed = false;
        //_particle3.useAutoRandomSeed = false;
    }

    public void nwknw()
    {
        //_particle.randomSeed = (uint)Random.Range(uint.MinValue, uint.MaxValue);
        
        //burst = _particle.emission.GetBurst(0);

        //_particle.emission.

        //_particle1.randomSeed = _particle.randomSeed;

        _particle.Play();
    }
}
