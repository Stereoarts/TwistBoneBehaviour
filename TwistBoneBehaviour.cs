// Copyright (c) 2017 Nora
// Released under the MIT license
// http://opensource.org/licenses/mit-license.php
using UnityEngine;

public class TwistBoneBehaviour : MonoBehaviour
{
	public Transform sourceFrom; // BTW: Arm
	public Transform sourceTo; // BTW: Elbow
	
	public bool isDebugResetInterlock;

	[System.Serializable]
	public struct Target
	{
		public Transform transform;
		public float rate;
		[System.NonSerialized]
		public Quaternion defaultLocalRotation;
		[System.NonSerialized]
		public bool defaultLocalRotationIsIdentity;
	}

	public Target[] targets;

	Vector3 _twistAxis;
	Quaternion _defaultLocalRotation;
	bool _defaultLocalRotationIsIdentity;

	void Awake()
	{
		if( sourceTo.parent = sourceFrom ) {
			_twistAxis = sourceTo.localPosition;
		} else { // Note: Must be A/T-posed.
			_twistAxis = sourceFrom.position - sourceTo.position;
			_twistAxis = (Inverse( sourceFrom.rotation ) * _twistAxis);
		}

		_defaultLocalRotation = sourceTo.localRotation;
		_defaultLocalRotationIsIdentity = FuzzyIdentity( _defaultLocalRotation );

		if( !_defaultLocalRotationIsIdentity ) {
			_twistAxis = Inverse( _defaultLocalRotation ) * _twistAxis;
		}

		SafeNormalize( ref _twistAxis );

		for( int i = 0, len = targets.Length; i < len; ++i ) {
			targets[i].defaultLocalRotation = targets[i].transform.localRotation;
			targets[i].defaultLocalRotationIsIdentity = FuzzyIdentity( targets[i].defaultLocalRotation );
		}
	}

	void LateUpdate()
	{
		if( isDebugResetInterlock ) {
			_ResetInterlock();
			return;
		}

		Quaternion tempRotation = sourceTo.localRotation;
		if( !_defaultLocalRotationIsIdentity ) {
			tempRotation = Inverse(_defaultLocalRotation) * tempRotation;
		}

		if( FuzzyIdentity( tempRotation ) ) {
			_ResetInterlock();
			return;
		}

		Vector3 tempAxis;
		float tempAngle;
		tempRotation.ToAngleAxis( out tempAngle, out tempAxis );

		float d = Vector3.Dot( tempAxis, _twistAxis );
		if( d >= 1.0f - _Epsilon || d <= -1.0f + _Epsilon ) {
			_Interlock( tempRotation );
			return;
		}

		tempAngle *= Mathf.Deg2Rad;
		if( tempAngle >= -_Epsilon && tempAngle <= _Epsilon ) {
			_ResetInterlock();
			return;
		}

		Vector3 rotateAxisFrom = Vector3.Cross(Vector3.Cross(_twistAxis, tempAxis), _twistAxis);
		if( !SafeNormalize( ref rotateAxisFrom ) ) {
			_ResetInterlock();
			return;
		}

		Vector3 rotateAxisTo = tempRotation * rotateAxisFrom;
		rotateAxisTo -= _twistAxis * Vector3.Dot( _twistAxis, rotateAxisTo );
		if( !SafeNormalize( ref rotateAxisTo ) ) {
			Debug.LogError( "rotateAxisTo is NaN" );
			return;
		}

		d = Vector3.Dot( rotateAxisFrom, rotateAxisTo );
		if( d >= 1.0f - _Epsilon ) {
			_ResetInterlock();
			return;
		}

		var r = Mathf.Acos( d );
		Vector3 axis = Vector3.Cross( rotateAxisFrom, rotateAxisTo );
		if( !SafeNormalize( ref axis ) ) {
			_ResetInterlock();
			return;
		}

		_Interlock( Quaternion.AngleAxis( r * Mathf.Rad2Deg, axis ) );
	}

	void _Interlock( Quaternion q )
	{
		for( int i = 0, len = targets.Length; i < len; ++i ) {
			Quaternion q2 = Quaternion.Slerp( Quaternion.identity, q, targets[i].rate );
			if( targets[i].defaultLocalRotationIsIdentity ) {
				targets[i].transform.localRotation = q2;
			} else {
				targets[i].transform.localRotation = targets[i].defaultLocalRotation * q2;
			}
		}
	}

	void _ResetInterlock()
	{
		for( int i = 0, len = targets.Length; i < len; ++i ) {
			targets[i].transform.localRotation = targets[i].defaultLocalRotation;
		}
	}

	// MathLib

	public static bool SafeNormalize( ref Vector3 v )
	{
		float lenSq = v.x * v.x + v.y * v.y + v.z * v.z;
		if( lenSq > _Epsilon ) {
			v = v *  (1.0f / Mathf.Sqrt(lenSq));
			return true;
		}

		return false;
	}

	public const float _Epsilon = 1e-9f;

	public static bool FuzzyIdentity( Quaternion q, float epsilon = _Epsilon )
	{
		return (q.x >= -epsilon && q.x <= epsilon) &&
			(q.y >= -epsilon && q.y <= epsilon) &&
			(q.z >= -epsilon && q.z <= epsilon) &&
			(q.w >= 1.0f - epsilon && q.w <= 1.0f + epsilon);
	}

	public static Quaternion Inverse( Quaternion q )
	{
		return new Quaternion( -q.x, -q.y, -q.z, q.w );
	}
}
