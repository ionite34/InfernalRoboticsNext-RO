﻿using System;

using KSP.IO;
using UnityEngine;

namespace InfernalRobotics_v3.Module
{
	class ModuleIRMovedPart : PartModule
	{
		/*
		 * Idea of the class
		 * 
		 * We keep all relative positions and rotations between the servos and the parts they
		 * move. Those values can then be used to calculate the (org-) positions and rotations
		 * without the need to first calculate back from the current state. This is done to
		 * minimize the potential failures due to floating point rounding.
		 * 
		 * Additionally we can keep information that nobody can calculate like the original
		 * orientation of the axis of a joint. If a part bends while it's used, those axis
		 * could point into wrong directions. This class helps to make everything more accurate.
		 */

		public Part rootPart = null;

		public Vessel relVessel = null;

		public Vector3 relPos = Vector3.zero;
		public Quaternion relRot = Quaternion.identity;

		public bool isFreePivot = false;
		public bool isServo = false;
		public bool isRotational;

		public Vector3 relPivot;
		public Vector3 relAxis;
		
		public Vector3 lastTrans = Vector3.zero;
		public Quaternion lastRot = Quaternion.identity;


		/*
		 * Search all relative positions and rotations that we need and call the function
		 * recursively on all children except those implementing ModuleIRServo_v3 because
		 * they will call this function themself.
		 */
		public static ModuleIRMovedPart InitializePart(Part part, Part root)
		{
			ModuleIRMovedPart module = part.GetComponent<ModuleIRMovedPart>();

			if(!module || (module.relVessel != part.vessel))
			{
				if(!module)
					module = part.gameObject.AddComponent<ModuleIRMovedPart>();

				module.relVessel = part.vessel;

				module.relPos = Quaternion.Inverse(root.orgRot) * (part.orgPos - root.orgPos);
				module.relRot = Quaternion.Inverse(root.orgRot) * part.orgRot;

				ModuleIRServo_v3 servo = part.GetComponent<ModuleIRServo_v3>();

				if(servo)
				{
					module.isServo = true;
					module.isRotational = servo.IsRotational;

					module.relPivot = module.relPos + Quaternion.Inverse(root.orgRot) * part.orgRot * part.attachJoint.Joint.anchor;

					module.relAxis = (Quaternion.Inverse(root.orgRot) * part.orgRot * part.attachJoint.Joint.axis).normalized;
				}
				else if(part.GetComponent<IJointLockState>() != null)
				{
					module.isFreePivot = true;
					module.isServo = false;
				}
				else
				{
					module.isFreePivot = false;
					module.isServo = false;
				}

				module.rootPart = root;
			}

			Part childRoot = (module.isServo || module.isFreePivot) ? part : root;

			foreach(Part child in part.children)
			{
				if(!child.GetComponent<ModuleIRServo_v3>())
					InitializePart(child, childRoot);
			}

			return module;
		}

		/*
		 * Find root (the real root or the first parent implementing ModuleIRServo_v3)
		 * and then call the real InitializePart function.
		 */
		public static ModuleIRMovedPart InitializePart(Part part)
		{
			Part root = part;

			do root = root.parent;
			while(!root.GetComponent<ModuleIRServo_v3>() && root.parent);

			return InitializePart(part, root);
		}


		public override void OnStart(StartState state)
		{
			GameEvents.onVesselWasModified.Add(OnVesselWasModified);
		}

		public void OnDestroy()
		{
			GameEvents.onVesselWasModified.Remove(OnVesselWasModified);
		}

		public void OnVesselWasModified(Vessel v)
		{
			if((part.vessel == v) && (relVessel != v))
				Destroy(this);
		}

		public void UpdatePosition()
		{
			// catch uninitialized state
			if(!rootPart)
				return;

			if(isServo)
			{
				if(isRotational)
				{
					part.orgPos = rootPart.orgPos + rootPart.orgRot * (relPivot + lastRot * (relPos - relPivot));
					part.orgRot = rootPart.orgRot * lastRot * relRot;
				}
				else
				{
					part.orgPos = rootPart.orgPos + rootPart.orgRot * (relPos + lastTrans);
					part.orgRot = rootPart.orgRot * relRot;
				}
			}
			else if(isFreePivot)
			{
				part.UpdateOrgPosAndRot(part.vessel.rootPart);
			}
			else
			{
				part.orgPos = rootPart.orgPos + rootPart.orgRot * relPos;
				part.orgRot = rootPart.orgRot * relRot;
			}

			for(int i = 0; i < part.children.Count; i++)
			{
				ModuleIRMovedPart child = part.children[i].GetComponent<ModuleIRMovedPart>();
				if(child)
					child.UpdatePosition();
			}
		}
	}
}
