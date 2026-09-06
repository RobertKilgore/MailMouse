using UnityEngine;

public class AddressRotation : MonoBehaviour
{
	private const string BillboardTargetTag = "Billboard Target";

	private Transform billboardTarget;
	private bool hasWarned;

	private void Awake()
	{
		FindBillboardTarget();
	}

	private void LateUpdate()
	{
		if (billboardTarget == null)
		{
			FindBillboardTarget();
			if (billboardTarget == null)
				return;
		}

		Debug.Log($"AddressRotation: Attempting to turn {gameObject.name} to match {billboardTarget.gameObject.name}.", this);
		transform.rotation = billboardTarget.rotation;
	}

	private void FindBillboardTarget()
	{
		GameObject target = GameObject.FindGameObjectWithTag(BillboardTargetTag);

		if (target != null)
		{
			billboardTarget = target.transform;
			return;
		}

		if (hasWarned == false)
		{
			Debug.LogWarning($"AddressRotation: No GameObject with the '{BillboardTargetTag}' tag was found.", this);
			hasWarned = true;
		}
	}
}

