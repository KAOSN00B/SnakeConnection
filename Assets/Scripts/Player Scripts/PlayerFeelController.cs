using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// =====================================================================
// PlayerFeelController.cs  —  Attach to the Player root GameObject.
//
// This script owns ALL player-specific game feel in one place:
//   1. Camera smooth follow with forward-look offset + gradual zoom-out
//   2. Speed spike when the player makes a sharp direction change
//   3. Passive speed escalation over the first 90 seconds of play
//   4. Subtle body tilt on turns (lean opposite to the turn direction)
//   5. Near-miss chromatic aberration pulse from enemy bullets
//   6. Line-renderer trail behind the player
//   7. Red vignette panic pulse when only 1-2 followers remain
//   8. Screen shake wired to damage, follower death, and shooting events
//
// AUDIO PLACEHOLDERS: Every spot that needs a sound is marked:
//   // AUDIO: description
//   // AudioManager.Instance?.Play("SoundName");
// Drop in your AudioManager calls there when you're ready.
//
// DEPENDENCIES (small edits needed — see comments on each method):
//   - MoveCamera  needs SetDynamicOffset(Vector3)
//   - PlayerMovement needs a public SpeedMultiplier float property
//   - ChainManager needs a public FollowerCount int property
// =====================================================================

[RequireComponent(typeof(Rigidbody))]
public class PlayerFeelController : MonoBehaviour
{
    // =====================================================================
    // INSPECTOR FIELDS
    // =====================================================================

    [Header("Player Death")]
    [Tooltip("Particle prefab that plays at the player's position when they die.")]
    [SerializeField] private GameObject _deathExplosionPrefab;
    [Tooltip("Fallback delay before showing the game over screen if no particle is assigned.")]
    [SerializeField] private float _deathDelay = 2f;
    [Tooltip("The Game Over panel — assign the inactive panel from the Canvas here.")]
    [SerializeField] private GameOverUI _gameOverUI;

    [Header("References")]
    [Tooltip("The child GameObject that is the player's visual mesh. Tilt is applied here.")]
    [SerializeField] private Transform _bodyTransform;

    [Tooltip("The MoveCamera script on the Camera Rig. Auto-found at runtime if left empty.")]
    [SerializeField] private MoveCamera _moveCamera;

    [Tooltip("URP Global Volume holding ChromaticAberration and Vignette overrides. Auto-found if empty.")]
    [SerializeField] private Volume _postProcessVolume;

    // ------------------------------------------------------------------
    // Camera settings
    // ------------------------------------------------------------------
    [Header("Camera — Forward Offset")]
    [Tooltip("How many world units ahead of the player the camera leads in the movement direction. " +
             "Gives the player more look-ahead space when running.")]
    [SerializeField] private float _cameraForwardOffset = 2.5f;

    [Tooltip("Speed at which the camera offset lerps toward its target. " +
             "8-12 feels smooth without being laggy. Higher = more snappy.")]
    [SerializeField] private float _cameraLerpSpeed = 9f;

    [Header("Camera — Zoom-Out Over Time")]
    [Tooltip("The camera's Y offset at game start. Should match whatever you have set in MoveCamera's offset.y (default 8.5).")]
    [SerializeField] private float _cameraStartY = 8.5f;

    [Tooltip("The camera's maximum Y offset, reached at _survivalDuration seconds. " +
             "A gentle 3-4 unit increase is enough — the player barely notices but it feels bigger.")]
    [SerializeField] private float _cameraMaxY = 12f;

    // ------------------------------------------------------------------
    // Speed settings
    // ------------------------------------------------------------------
    [Header("Speed — Passive Escalation")]
    [Tooltip("How many seconds before the player reaches maximum speed. 90 feels slow and fair.")]
    [SerializeField] private float _survivalDuration = 90f;

    [Tooltip("The speed multiplier the player reaches at _survivalDuration seconds. " +
             "1.3 = 30% faster. The player never reads the number — they just feel faster.")]
    [SerializeField] private float _maxSpeedMultiplier = 1.30f;

    [Header("Speed — Direction-Change Spike")]
    [Tooltip("Short speed burst applied when the player makes a sharp directional change. " +
             "1.18 = 18% faster for _spikeDuration seconds.")]
    [SerializeField] private float _spikeMultiplier = 1.18f;

    [Tooltip("How long the direction-change speed spike lasts in seconds.")]
    [SerializeField] private float _spikeDuration = 0.1f;

    [Tooltip("Dot product below this value is considered a 'sharp turn' and triggers the spike. " +
             "0 = only 90-degree+ turns spike. -0.3 = gentler turns also spike.")]
    [SerializeField] private float _sharpTurnDotThreshold = 0f;

    // ------------------------------------------------------------------
    // Body tilt settings
    // ------------------------------------------------------------------
    [Header("Body Tilt on Turns")]
    [Tooltip("Maximum lean angle in degrees when the player makes a sharp turn. " +
             "5-8 degrees is subtle enough to look physical without looking broken.")]
    [SerializeField] private float _maxTiltDegrees = 6f;

    [Tooltip("How quickly the body snaps to and from the lean angle. " +
             "10 is snappy; lower values make it feel heavier.")]
    [SerializeField] private float _tiltLerpSpeed = 10f;

    // ------------------------------------------------------------------
    // Near-miss chromatic aberration
    // ------------------------------------------------------------------
    [Header("Near-Miss Effect")]
    [Tooltip("If an enemy bullet passes within this many units of the player, the near-miss effect fires.")]
    [SerializeField] private float _nearMissRadius = 2f;

    [Tooltip("Peak chromatic aberration intensity on a near-miss. 0 = none, 1 = full distortion.")]
    [SerializeField] private float _nearMissChromaticPeak = 0.65f;

    [Tooltip("Seconds for chromatic aberration to fade back to 0 after a near-miss triggers.")]
    [SerializeField] private float _nearMissFadeTime = 0.25f;

    // ------------------------------------------------------------------
    // Trail settings
    // ------------------------------------------------------------------
    [Header("Player Trail")]
    [Tooltip("Width of the trail ribbon at the player end. Tapers to 0 at the tail end.")]
    [SerializeField] private float _trailStartWidth = 0.35f;

    [Tooltip("Number of world positions stored in the trail. More = longer, uses more memory.")]
    [SerializeField] private int _trailLength = 25;

    [Tooltip("Seconds between trail position samples. 0.03 is smooth without being expensive.")]
    [SerializeField] private float _trailSampleRate = 0.03f;

    [Tooltip("Color and alpha of the trail at the head (newest end). The tail fades to fully transparent.")]
    [SerializeField] private Color _trailColor = new Color(0.3f, 0.85f, 1f, 0.55f);

    // ------------------------------------------------------------------
    // Panic vignette settings
    // ------------------------------------------------------------------
    [Header("Low-Follower Panic Vignette")]
    [Tooltip("Vignette starts pulsing when the follower count is at or below this number.")]
    [SerializeField] private int _panicFollowerThreshold = 2;

    [Tooltip("Duration of one full vignette pulse cycle in seconds. 0.8 feels like a heartbeat.")]
    [SerializeField] private float _vignettePulsePeriod = 0.8f;

    [Tooltip("Peak vignette intensity during a panic pulse. 0.3 is visible without being annoying.")]
    [SerializeField] private float _vignetteMaxIntensity = 0.30f;

    [Tooltip("Color of the panic vignette. Red signals danger.")]
    [SerializeField] private Color _vignetteColor = Color.red;

    // =====================================================================
    // PRIVATE STATE
    // =====================================================================

    // Component cache — grabbed once in Awake
    private Rigidbody       _rb;
    private PlayerMovement  _playerMovement;
    private Health          _playerHealth;

    // Camera state
    // _currentCamOffset is a DELTA added on top of MoveCamera's serialized base offset.
    // It starts at zero so the camera starts exactly where MoveCamera has it set.
    private Vector3 _currentCamOffset;

    // Speed state
    private float _gameTimer;   // Seconds elapsed since the scene loaded
    private bool  _isSpiking;   // True while a direction-change burst is active

    // Body tilt state
    private Vector3 _prevFlatVelocity;  // Last frame's XZ velocity — used to detect direction changes
    private float   _currentTiltZ;      // Current Z-axis lean angle (smoothed)

    // Trail state
    private LineRenderer   _trail;
    private Queue<Vector3> _trailQueue    = new Queue<Vector3>();
    private Vector3[]      _trailBuffer;  // pre-allocated; CopyTo into this avoids ToArray() allocation
    private float          _trailTimer;
    private Gradient       _cachedGradient;

    // Post-processing state
    private ChromaticAberration _chromatic;
    private Vignette            _vignette;
    private bool                _ppReady;          // False if no volume / components found
    private float               _chromaticTarget;  // Current target value being faded toward

    // Panic vignette state
    private float _panicTimer;
    private bool  _panicWasActive;
    // True once the follower count has exceeded the threshold — prevents panic from
    // firing at the very start of the game when the player only has 1-2 followers
    private bool  _hasExceededThreshold;

    // =====================================================================
    // UNITY LIFECYCLE
    // =====================================================================

    private void Awake()
    {
        _rb             = GetComponent<Rigidbody>();
        _playerMovement = GetComponent<PlayerMovement>();

        // Health lives on the body child, not the player root — use InChildren
        _playerHealth = GetComponentInChildren<Health>();

        // Auto-locate MoveCamera if the Inspector field was left empty
        if (_moveCamera == null)
            _moveCamera = FindAnyObjectByType<MoveCamera>();

        // Auto-locate GameOverUI in the scene if not assigned
        if (_gameOverUI == null)
            _gameOverUI = FindAnyObjectByType<GameOverUI>(FindObjectsInactive.Include);

        // Try to connect post-processing (logs warnings if not found — non-fatal)
SetupPostProcessing();

        // Create the trail line renderer as a child object
        SetupTrail();

        // Start the camera delta at zero so it doesn't pop on the first frame
        _currentCamOffset = Vector3.zero;
    }

    private void OnEnable()
    {
        if (_playerHealth != null)
            _playerHealth.OnHealthChanged += OnPlayerHealthChanged;
        Health.OnPlayerDeath += HandlePlayerDeath;
    }

    private void OnDisable()
    {
        if (_playerHealth != null)
            _playerHealth.OnHealthChanged -= OnPlayerHealthChanged;
        Health.OnPlayerDeath -= HandlePlayerDeath;
    }

    private void Update()
    {
        _gameTimer += Time.deltaTime;

        // survivalProgress = 0 at game start, 1 when _survivalDuration seconds have passed
        float survivalProgress = Mathf.Clamp01(_gameTimer / _survivalDuration);

        UpdateCamera(survivalProgress);
        UpdateSpeedEscalation(survivalProgress);
        UpdateBodyTilt();       // Also calls CheckDirectionChangeSpike internally
        UpdateTrail();
        UpdatePanicVignette();
        UpdateChromaticDecay();
    }

    // =====================================================================
    // SETUP — called once from Awake
    // =====================================================================

    private void SetupPostProcessing()
    {
        if (_postProcessVolume == null)
            _postProcessVolume = FindAnyObjectByType<Volume>();

        if (_postProcessVolume == null)
        {
            Debug.LogWarning("[PlayerFeel] No URP Global Volume found. " +
                             "Near-miss and panic vignette effects are disabled. " +
                             "Add a Global Volume with a ChromaticAberration and Vignette override to your scene.");
            return;
        }

        // TryGet returns false (and leaves the out param null) if the profile doesn't have that override
        _postProcessVolume.profile.TryGet(out _chromatic);
        _postProcessVolume.profile.TryGet(out _vignette);

        _ppReady = true;

    }

    private void SetupTrail()
    {
        // Put the LineRenderer on its own child so it doesn't interfere with any other
        // components on the player object
        var trailObject = new GameObject("PlayerTrail");
        trailObject.transform.SetParent(transform);
        trailObject.transform.localPosition = Vector3.zero;

        _trail = trailObject.AddComponent<LineRenderer>();
        _trail.useWorldSpace        = true;
        _trail.startWidth           = _trailStartWidth;
        _trail.endWidth             = 0f;   // Taper to a point at the oldest (tail) end
        _trail.positionCount        = 0;
        _trail.receiveShadows       = false;
        _trail.shadowCastingMode    = UnityEngine.Rendering.ShadowCastingMode.Off;
        _trail.generateLightingData = false;

        // Sprites/Default supports vertex colors and looks neon without a custom shader.
        // Swap this for an additive shader if you want it to glow through other geometry.
        var trailMaterial = new Material(Shader.Find("Sprites/Default"));
        _trail.material = trailMaterial;

        // Build the fade gradient ONCE and cache it.
        // The tail (index 0, oldest position) is fully transparent.
        // The head (index 1, newest position) is the full _trailColor alpha.
        _cachedGradient = new Gradient();
        _cachedGradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(_trailColor, 0f),
                new GradientColorKey(_trailColor, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f,             0f),  // Tail — invisible
                new GradientAlphaKey(_trailColor.a,  1f)   // Head — full opacity
            }
        );
        _trail.colorGradient = _cachedGradient;

        // Pre-allocate the position buffer to the maximum trail length.
        // CopyTo() into this buffer avoids the per-frame allocation that ToArray() causes.
        _trailBuffer = new Vector3[_trailLength];
    }

    // =====================================================================
    // CAMERA FEEL
    // =====================================================================

    private void UpdateCamera(float survivalProgress)
    {
        if (_moveCamera == null) return;

        // --- Forward look-ahead ---
        // Shift the camera toward where the player is going so they can see threats earlier.
        // We read the actual Rigidbody velocity rather than input so it only nudges when moving.
        Vector3 flatVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        Vector3 moveDir      = flatVelocity.sqrMagnitude > 0.01f ? flatVelocity.normalized : Vector3.zero;
        Vector3 forwardNudge = moveDir * _cameraForwardOffset;

        // --- Gradual zoom-out ---
        // We express this as a DELTA on top of MoveCamera's base Y offset, starting at 0.
        // That way MoveCamera's serialized offset is still the source of truth for the home position.
        // At survivalProgress=0 this adds 0; at survivalProgress=1 it adds (_cameraMaxY - _cameraStartY) units.
        float deltaY = Mathf.Lerp(0f, _cameraMaxY - _cameraStartY, survivalProgress);

        // Combine the two effects: XZ nudge forward, Y creep upward
        Vector3 targetDelta = new Vector3(forwardNudge.x, deltaY, forwardNudge.z);

        // Lerp so the camera doesn't snap on direction changes — the lerp is the "smoothness"
        _currentCamOffset = Vector3.Lerp(_currentCamOffset, targetDelta, Time.deltaTime * _cameraLerpSpeed);

        // Hand off to MoveCamera — see the small edit needed in MoveCamera.cs below
        // MoveCamera.SetDynamicOffset(Vector3) adds this on top of its serialized base offset
        _moveCamera.SetDynamicOffset(_currentCamOffset);
    }

    // =====================================================================
    // SPEED ESCALATION
    // =====================================================================

    private void UpdateSpeedEscalation(float survivalProgress)
    {
        if (_playerMovement == null || _isSpiking) return;

        // Ramp the speed multiplier silently in the background.
        // The player feels faster without being told — it's a reward for surviving.
        _playerMovement.SpeedMultiplier = Mathf.Lerp(1f, _maxSpeedMultiplier, survivalProgress);

        // AUDIO: Gradually shift the pitch of any movement sounds here
        // AudioManager.Instance?.SetPitch("FootstepLoop", Mathf.Lerp(1f, 1.2f, t));
    }

    // Checks if the current velocity represents a sharp turn vs. the previous frame.
    // Called inside UpdateBodyTilt() which already reads velocity — avoids a second GetComponent call.
    private void CheckDirectionChangeSpike(Vector3 currentFlat)
    {
        // Need both frames to have actual movement before comparing directions
        if (currentFlat.sqrMagnitude < 0.01f || _prevFlatVelocity.sqrMagnitude < 0.01f) return;
        if (_isSpiking) return;

        float dot = Vector3.Dot(_prevFlatVelocity.normalized, currentFlat.normalized);

        // Dot < threshold means the angle between last frame and this frame is wide enough
        // to count as a "sharp" turn — fire the speed burst
        if (dot < _sharpTurnDotThreshold)
            StartCoroutine(SpeedSpikeRoutine());
    }

    private IEnumerator SpeedSpikeRoutine()
    {
        _isSpiking = true;

        // Stack the spike on top of the current escalation so the two effects compound
        float escalation = Mathf.Lerp(1f, _maxSpeedMultiplier, Mathf.Clamp01(_gameTimer / _survivalDuration));
        if (_playerMovement != null)
            _playerMovement.SpeedMultiplier = _spikeMultiplier * escalation;

        // AUDIO: Short "burst" accent — footstep accent, momentum whoosh, etc.
        // AudioManager.Instance?.Play("SpeedBurst");

        yield return new WaitForSeconds(_spikeDuration);

        _isSpiking = false;
        // Normal escalation is restored at the top of the next UpdateSpeedEscalation call
    }

    // =====================================================================
    // BODY TILT ON TURNS
    // =====================================================================

    private void UpdateBodyTilt()
    {
        // Read current flat (XZ) velocity — this is the only place we read it so we might as well
        // also trigger the direction-change spike check here
        Vector3 flatVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        CheckDirectionChangeSpike(flatVelocity);

        float targetTilt = 0f;

        if (flatVelocity.sqrMagnitude > 0.01f && _prevFlatVelocity.sqrMagnitude > 0.01f)
        {
            // Cross product between last-frame and this-frame movement direction.
            // The Y component of the cross tells us which way the player turned:
            //   positive Y = turning left  → lean left (negative Z in local space)
            //   negative Y = turning right → lean right (positive Z in local space)
            // We negate it so the lean is OPPOSITE the turn direction (inertia / weight shift feel)
            Vector3 cross = Vector3.Cross(_prevFlatVelocity.normalized, flatVelocity.normalized);
            targetTilt = -cross.y * _maxTiltDegrees;
        }
        // else: player is stationary or just started moving — target stays 0, lerp back to upright

        // Smooth the tilt so it eases in and out rather than snapping
        _currentTiltZ = Mathf.Lerp(_currentTiltZ, targetTilt, Time.deltaTime * _tiltLerpSpeed);

        // Apply ONLY to the body child as a local-space Z rotation.
        // The root object is rotated toward the mouse by PlayerMovement.AimAtMouse() each frame.
        // Setting local rotation here means: "same facing direction as parent, but tilted this much."
        // NOTE: The body's base local rotation should be identity (no pre-rotation in the prefab)
        //       for this to look correct. If the mesh faces a different direction, bake that offset
        //       into the mesh in your modelling tool rather than in the prefab transform.
        if (_bodyTransform != null)
        {
            // Preserve the Y rotation set by AimAtMouse — only override Z for the lean.
            // If _bodyTransform is the same object PlayerMovement rotates, writing
            // Euler(0,0,tilt) would zero out the mouse-aim Y rotation every frame (the shooting bug).
            float aimY = _bodyTransform.eulerAngles.y;
            _bodyTransform.rotation = Quaternion.Euler(0f, aimY, _currentTiltZ);
        }

        _prevFlatVelocity = flatVelocity;
    }

    // =====================================================================
    // PLAYER TRAIL
    // =====================================================================

    private void UpdateTrail()
    {
        _trailTimer += Time.deltaTime;

        // Sample the player's world position at fixed intervals rather than every frame.
        // This keeps the trail length consistent regardless of frame rate.
        if (_trailTimer >= _trailSampleRate)
        {
            _trailTimer = 0f;

            // Store at the player's feet (keep Y from the player's actual position)
            _trailQueue.Enqueue(transform.position);

            // Remove the oldest point once we hit the cap
            while (_trailQueue.Count > _trailLength)
                _trailQueue.Dequeue();
        }

        // CopyTo writes queue elements (oldest→newest) into the pre-allocated buffer.
        // This avoids the heap allocation that ToArray() causes every frame.
        int count = _trailQueue.Count;
        _trailQueue.CopyTo(_trailBuffer, 0);
        _trail.positionCount = count;
        if (count > 0)
            _trail.SetPositions(_trailBuffer); // LineRenderer only reads up to positionCount elements
    }

    // =====================================================================
    // NEAR-MISS CHROMATIC ABERRATION
    // =====================================================================

    // PUBLIC — call this from Bullet.cs when an enemy bullet (no owner set) passes close to the player.
    //
    // Example integration in Bullet.cs, inside OnTriggerExit or in FixedUpdate while the bullet is alive:
    //
    //   if (_owner == null)  // enemy bullet
    //   {
    //       GameObject player = GameObject.FindWithTag("Player");
    //       if (player != null)
    //       {
    //           float dist = Vector3.Distance(transform.position, player.transform.position);
    //           var feel = player.GetComponent<PlayerFeelController>();
    //           if (feel != null && dist < feel.NearMissRadius)
    //               feel.TriggerNearMiss();
    //       }
    //   }
    //
    // Alternatively, add a large sphere trigger to the player tagged "NearMissZone" and
    // call TriggerNearMiss() from OnTriggerExit when a bullet leaves that zone without hitting anything.
    public void TriggerNearMiss()
    {
        // Snap aberration to its peak — the decay routine brings it back smoothly each frame
        _chromaticTarget = _nearMissChromaticPeak;

        // Tiny camera micro-shake so the effect hits multiple senses at once
        _moveCamera?.AddShake(0.08f, 0.04f);

        // AUDIO: Short tight "swish" or "whoosh" — the sound of something barely missing
        // AudioManager.Instance?.Play("NearMiss");
    }

    // Exposes the radius so Bullet.cs can read it without a magic number
    public float NearMissRadius => _nearMissRadius;

    private void UpdateChromaticDecay()
    {
        if (!_ppReady || _chromatic == null || _chromaticTarget <= 0f) return;

        // Linear fade back toward 0 — simple and predictable
        _chromaticTarget -= Time.deltaTime / _nearMissFadeTime;
        _chromaticTarget  = Mathf.Max(_chromaticTarget, 0f);

        _chromatic.intensity.Override(_chromaticTarget);
    }

    // =====================================================================
    // PANIC VIGNETTE (low follower count)
    // =====================================================================

    private void UpdatePanicVignette()
    {
        if (!_ppReady || _vignette == null) return;
        if (ChainManager.Instance == null) return;

        int count = ChainManager.Instance.FollowerCount;

        // Only unlock panic once the player has had MORE than the threshold —
        // so 1-2 followers at the start of the game doesn't trigger it
        if (count > _panicFollowerThreshold)
            _hasExceededThreshold = true;

        // Pulse when in danger, but NOT when the chain is completely empty —
        // at that point the player is about to die and the game handles that separately
        bool shouldPanic = _hasExceededThreshold && count > 0 && count <= _panicFollowerThreshold;

        if (shouldPanic)
        {
            if (!_panicWasActive)
            {
                // First frame of panic — start with a clean timer and set the color
                _panicWasActive = true;
                _panicTimer     = 0f;
                _vignette.color.Override(_vignetteColor);

                // AUDIO: Begin looping heartbeat or low-health ambient drone
                // AudioManager.Instance?.PlayLoop("LowFollowerHeartbeat");
            }

            _panicTimer += Time.deltaTime;

            // Sine wave oscillates between 0 and 1 at a rate of one cycle per _vignettePulsePeriod seconds.
            // (sin + 1) / 2 remaps [-1,1] to [0,1] so the vignette never goes negative.
            float sineValue = (Mathf.Sin(_panicTimer * (Mathf.PI * 2f / _vignettePulsePeriod)) + 1f) * 0.5f;
            _vignette.intensity.Override(sineValue * _vignetteMaxIntensity);
        }
        else if (_panicWasActive)
        {
            // Just left panic (gained a follower, or all followers died triggering game-over)
            _panicWasActive = false;
            _panicTimer     = 0f;
            _vignette.intensity.Override(0f);

            // AUDIO: Stop the heartbeat loop
            // AudioManager.Instance?.StopLoop("LowFollowerHeartbeat");
        }
    }

    // =====================================================================
    // SCREEN SHAKE HOOKS
    // =====================================================================

    // Called automatically by the Health event subscription (OnEnable / OnDisable above)
    private void OnPlayerHealthChanged(int current, int max)
    {
        // Only react to damage (current dropped), not healing
        if (current >= max) return;

        _moveCamera?.AddShake(0.3f, 0.2f);

        // Also spike chromatic aberration so damage registers in two senses
        if (_ppReady && _chromatic != null)
            _chromaticTarget = Mathf.Max(_chromaticTarget, 0.45f);

        // AUDIO: Player hit grunt or impact sound
        // AudioManager.Instance?.Play("PlayerHit");
    }

    // PUBLIC — call this from FollowerDeathFeedback.TriggerDeath() when a follower dies.
    // Pass wasLastFollower = true when ChainManager.FollowerCount will be 0 after this death.
    public void NotifyFollowerDied(bool wasLastFollower)
    {
        // The last follower dying is a big moment — longer, stronger shake to underline it
        float duration  = wasLastFollower ? 0.6f : 0.4f;
        float magnitude = wasLastFollower ? 0.4f : 0.25f;

        _moveCamera?.AddShake(duration, magnitude);

        // Big chromatic blast when the last follower goes — feels like a gut punch
        if (wasLastFollower && _ppReady && _chromatic != null)
            _chromaticTarget = Mathf.Max(_chromaticTarget, 0.85f);

        // AUDIO: Follower death sound — louder / lower pitch if it's the last one
        // AudioManager.Instance?.Play(wasLastFollower ? "LastFollowerDie" : "FollowerDie");
    }

    // PUBLIC — call this from your shooting script when the player or a follower fires a bullet.
    // The tiny recoil shake makes shooting feel physical without being distracting.
    public void NotifyPlayerShot()
    {
        _moveCamera?.AddShake(0.05f, 0.05f);

        // AUDIO: Player shoot sound
        // AudioManager.Instance?.Play("PlayerShoot");
    }

    // =====================================================================
    // PLAYER DEATH
    // =====================================================================

    private void HandlePlayerDeath()
    {
        StartCoroutine(PlayerDeathRoutine());
    }

    private IEnumerator PlayerDeathRoutine()
    {
        float delay = _deathDelay;

        // This script lives on the player root, so transform.position is always exact
        if (_deathExplosionPrefab != null)
        {
            GameObject explosion = Instantiate(_deathExplosionPrefab, transform.position, Quaternion.identity);

            ParticleSystem particle = explosion.GetComponent<ParticleSystem>();
            if (particle != null && !particle.main.loop)
            {
                // Force unscaled time so the particle plays even after Time.timeScale = 0
                var main = particle.main;
                main.useUnscaledTime = true;
                delay = particle.main.duration;
            }
        }

        // Hide only the visuals and disable physics instead of the whole GameObject
        // so this coroutine can finish and show the Game Over screen.
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers) r.enabled = false;
        
        if (TryGetComponent<Collider>(out var col)) col.enabled = false;
        if (TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;

        // Stop shake before freezing — ShakeRoutine uses Time.deltaTime which becomes 0
// at timeScale 0, so any active shake would loop forever without this
        _moveCamera?.StopShake();

        // Freeze everything — enemies, spawners, bullets all stop
        Time.timeScale = 0f;

        // WaitForSecondsRealtime ignores timeScale so the delay still counts down
        yield return new WaitForSecondsRealtime(delay);

        _gameOverUI?.Show();
    }
}
