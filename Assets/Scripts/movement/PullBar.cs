using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PullBar : MonoBehaviour {
  public Slider bar;
  public Grappling grappling;
  private void Start() {
    grappling = GameObject.FindGameObjectWithTag("Player").GetComponent<Grappling>();
    bar = GetComponent<Slider>();
    bar.maxValue = grappling.pullBudget;
    bar.value = grappling.pullBudget;
  }
  public void SetPull(float time) {
    bar.value = time;
  }
}
