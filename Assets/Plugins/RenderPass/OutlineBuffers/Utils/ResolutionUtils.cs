using System;
using System.Collections.Generic;
using UnityEngine;

public class ResolutionUtils
{
    ///<summary> FUHD (Full Ultra HD) / 8K 4320p </summary>
    static readonly Resolution _FUHD = new() {width = 7680, height = 4320};

    /// <inheritdoc cref="_FUHD"/>
    static readonly Resolution _8K = _FUHD;

    /// <inheritdoc cref="_FUHD"/>
    static readonly Resolution _4320p = _FUHD;

    static readonly Resolution _DCI4K = new() {width = 4096, height = 2304};

    ///<summary> UHD (Ultra HD) / 4K 2160p </summary>
    static readonly Resolution _UHD = new() {width = 3840, height = 2160};

    /// <inheritdoc cref="_UHD"/>
    static readonly Resolution _4K = _UHD;

    /// <inheritdoc cref="_UHD"/>
    static readonly Resolution _2160p = _UHD;

    ///<summary> QHD (Quad HD) / 2K 1440p </summary>
    static readonly Resolution _QHD = new() {width = 2560, height = 1440};

    /// <inheritdoc cref="_QHD"/>
    static readonly Resolution _2K = _QHD;

    /// <inheritdoc cref="_QHD"/>
    static readonly Resolution _1440p = _QHD;

    ///<summary> FHD (Full HD) / 1080p </summary>
    static readonly Resolution _FHD = new() {width = 1920, height = 1080};

    /// <inheritdoc cref="FHD"/>
    static readonly Resolution _1080p = _FHD;

    // HD
    static readonly Resolution _HD = new() {width = 1280, height = 720};
    static readonly Resolution _720p = _HD;

    // static Resolution  _540p = new() { width = 960,  height = 540 };  // qHD
    // static Resolution  _480p = new() { width = 854,  height = 480 };  // SD
    // static Resolution  _360p = new() { width = 640,  height = 360 };
    // static Resolution  _240p = new() { width = 426,  height = 240 };

    private Resolution[] _commonResolutions = new[]
    {
        _FUHD, _UHD, _QHD, _FHD
    };

    static Resolution _hardwareResolution = Screen.currentResolution;
    readonly Vector2 _hardwareScreenSize = new(_hardwareResolution.width, _hardwareResolution.height);
    public int hardwareMaxMip;

    void SetHardwareDownsampleLimit()
    {
        hardwareMaxMip = _hardwareScreenSize switch
        {
            _ when Equals(_FUHD.width, _FUHD.height)   => 5,
            _ when Equals(_DCI4K.width, _DCI4K.height) => 9,
            _ when Equals(_UHD.width, _UHD.height)     => 5,
            _ when Equals(_QHD.width, _QHD.height)     => 6,
            _ when Equals(_FHD.width, _FHD.height)     => 4,
            _ when Equals(_HD.width, _HD.height)       => 5,
            _                                          => hardwareMaxMip
        };
    }
}