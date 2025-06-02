using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Footsteps : MonoBehaviour
{
  [Tooltip("footstep clips to choose from.")]
  [SerializeField] private AudioClip[] clips;
  [SerializeField] private float footstepDelay = 0.75f;
  private float footstepDelayTimer = 0;

  private AudioSource source;

  void Awake() {
    source = GetComponent<AudioSource>();
    if (clips == null || clips.Length == 0)
      Debug.LogWarning($"No clips assigned on {gameObject.name}.");
  }

  void Update() {
    if (footstepDelayTimer > 0)
      footstepDelayTimer -= Time.deltaTime;
  }

  public void PlayFootstep() {
    if (footstepDelayTimer <= 0) {
      source.PlayOneShot(clips[Random.Range(0, clips.Length)]);
      footstepDelayTimer = footstepDelay;
    }
  }
}
