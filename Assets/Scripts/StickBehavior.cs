// Make a GameObject into a "marker" or "laser" that can draw on a the
// PaintableTexture in the scene.  Marks are drawn where a ray from the
// GameObject hits something which is paintable.  The ray is in the direction of
// the object's local "up".

// User interface logic lives elsewhere; this script mostly adds public methods
// to the GameObject to perform functions related to drawing.  Hiding or showing
// the GameObject is also supported.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class StickBehavior : MonoBehaviour {

    // Allows for ability to check if item has been grabbed.
    public OVRGrabbable gb;
    public OVRGrabber rightHand;
    public OVRGrabber leftHand;
    // Removes issues when throwing causes multiple 'fall-throughs' otherwise a new vector is created every drop
    private Vector3 respawn;

    // #GETRIDOF Might get rid of due to Alex's implementation
    //public GameObject laserPointer;
    //private LaserOnOff laser;

    public OVRInput.Controller rightController;
    public OVRInput.Controller leftController;
	public float maxDist = Mathf.Infinity;
	public Material inactiveBeamMaterial;
	public Material activeBeamMaterial;
    public Material drillBeamMaterial;
	private Renderer[] rends;
	private Renderer beamRenderer;
	private Rigidbody beamRB;
	private bool isActive = false;  // When "active", the laser draws on things and presses buttons
	private bool isVisible = true;
	private bool drill = false; // not supported yet
	private PaintableTexture pt;
	private LayerMask layerMask = Physics.DefaultRaycastLayers;
    private LayerMask colorMask = Physics.DefaultRaycastLayers;
    private LayerMask textureColorMask = Physics.DefaultRaycastLayers;
    private LayerMask drillMask = Physics.DefaultRaycastLayers;
    private LayerMask invisibleMask = Physics.DefaultRaycastLayers;
    private float maxForward = 10.0f;

    // For interpolated drawing
    private Vector3 lastDirection;
    private Vector3 lastPosition = new Vector3(float.NaN, float.NaN, float.NaN);

    void Start () {
        // Exact spot of the tool platform. Will change when we change where the tool platform goes.
        respawn = new Vector3(-0.71f, 0.25f, -7.09f);
        rends = gameObject.GetComponentsInChildren<Renderer>();
		// Retrieve the one and only instance of PaintableTexture (singleton pattern)
        pt = PaintableTexture.Instance;
		// Paintable objects must all live in a layer called "Paintable"
		layerMask = (1 << LayerMask.NameToLayer("Paintable"));
        // colorMask is to get the color from a single-colored material
        colorMask = (1 << LayerMask.NameToLayer("Color"));
        // textureColorMask is for getting a color from a texture
        textureColorMask = (1 << LayerMask.NameToLayer("TextureColor"));
        // invisibleMask is to set the alpha to 0 for a transparent collor
        invisibleMask = (1 << LayerMask.NameToLayer("Transparent"));       
        // drillMask is to toggle the drill bool
        drillMask = (1 << LayerMask.NameToLayer("Drill"));
        if (maxDist < maxForward)
			maxForward = maxDist;
		// Find the beam object.  It must have the "Beam" tag.
		foreach (Renderer r in rends) {
			if (r.gameObject.CompareTag("Beam")) {
				beamRenderer = r;
				beamRB = r.gameObject.GetComponent<Rigidbody>();
				break;
			}
		}
		if (beamRenderer == null) {
			Debug.Log("StickBehavior did not find a beam object (wrong material set?)");
		}
		makeInvisible();
		makeInactive();
	}

    private void updateMaterial()
    {
        if (beamRenderer != null) {
            if (isActive) {
                if (drill) {
                    beamRenderer.material = drillBeamMaterial;
                } else {
                    beamRenderer.material = activeBeamMaterial;
                }
            }  else {
                beamRenderer.material = inactiveBeamMaterial;
            }
        }
    }

    private void setCollisionDetection(bool s)
    {
        if (beamRB != null) {
            beamRB.detectCollisions = s;
            beamRB.WakeUp();
        }
    }

    private void enableCollisions()
    {
        setCollisionDetection(true);
    }

    private void disableCollisions()
    {
        setCollisionDetection(false);
    }

	// Set whether the GameObject is currently active (drawing, pressing buttons, etc)
	public void setActive(bool d)
	{
		isActive = d;

        updateMaterial();
        setCollisionDetection(isActive);
    	
        if (!isActive) {
            lastPosition = new Vector3(float.NaN, float.NaN, float.NaN);
        }
	}

	public void makeActive()
	{
		setActive(true);
	}

	public void makeInactive()
	{
		setActive(false);
	}

	public void setDrill(bool d)
	{
		drill = d;
        updateMaterial();
	}

	// Make GameObject and its children visible or invisible.
	public void setVisibility(bool b)
	{	
		isVisible = b;
		foreach (Renderer r in rends) {
            r.enabled = b;
        }
    }

    public bool visible()
    {
        return isVisible;
    }

	public void makeInvisible()
	{
		setVisibility(false);
    }

	public void makeVisible() {
		setVisibility(true);
	}

    public bool isGrabbed()
    {
        if (gb != null)
            return gb.isGrabbed;
        else
            return true;  // If OVR grabbing not present, we assume it is always in hand.
    }

    // Respawns the controller if it falls to the floor.
    // Quick-and-dirty method that is less prone to bugs than utilizing an onTriggerEnter for the floor
    public void respawnLaser()
    {
        Rigidbody body = gameObject.GetComponent<Rigidbody>();
        body.isKinematic = true;
        transform.position = respawn;
        body.isKinematic = false;
       // laser.turnOff();
    }
    
    public string whichGrabbed()
    {
        if (gb.grabbedBy == rightHand)
            return "right";
        else
            return "left";
    }


    void Update()
    {
        // If the laser pointer falls down to a certain point, we respawn it at the original point on the map.
        if ((transform.position.y < -2.6f) && (!isGrabbed()))
        {
            respawnLaser();
        }

        // Necessary for getting input from the touch controllers
        OVRInput.Update();
        // Keeping as legacy from laser pointer as hand
        // transform.localPosition = OVRInput.GetLocalControllerPosition(Controller);
        // transform.localRotation = OVRInput.GetLocalControllerRotation(Controller) * Quaternion.Euler(90, 0, 0);

        if (isGrabbed())
        {
            if (gb.grabbedBy == rightHand)
            {
                // DO NOT CHANGE 
                // The way the laser pointer was made doesn't allow for the usual 'snap offset'
                Vector3 rightHandSnap = new Vector3(-0.58f, 0.96f, -7.76f);
                transform.localPosition = OVRInput.GetLocalControllerPosition(rightController) + rightHandSnap;
                transform.localRotation = OVRInput.GetLocalControllerRotation(rightController) * Quaternion.Euler(90, 0, 0);
            }
            else
            {
                Vector3 leftHandSnap = new Vector3(-0.62f, 0.96f, -7.76f);
                transform.localPosition = OVRInput.GetLocalControllerPosition(leftController) + leftHandSnap;
                transform.localRotation = OVRInput.GetLocalControllerRotation(leftController) * Quaternion.Euler(90, 0, 0);
            }

            if (isActive)
            {
                PaintWithInterpolation();
            }

            // checks if we were drawing in the previous scene
            if (isActive)
            {
                this.lastPosition = transform.position;
                this.lastDirection = transform.TransformDirection(Vector3.up);
            }
        }

        if (isActive) {
            if (!isGrabbed()) {
                // Beam was on when grabbing stopped; turn it off.
                makeInactive();
            }
        }

    }

    bool HaveLastPosition()
    {
        return !(float.IsNaN(lastPosition.x) || float.IsNaN(lastPosition.y) || float.IsNaN(lastPosition.z));
    }

    void PaintWithInterpolation()
    {
        //threshold for where linear interpolation doesn't affect performance.
        //Still see some spotting if drawing on edges of the H2View
        float maxStep = 0.25f;
        Vector3 curDirection = transform.TransformDirection(Vector3.up);

        if (HaveLastPosition()) {
            float spaceDist = Vector3.Distance(lastPosition, transform.position);
            float angleDist = Vector3.Angle(lastDirection, curDirection);

            float numSteps = Mathf.Ceil((spaceDist + angleDist) / maxStep);

            for (int j = 1; j <= numSteps; j++) {
                var pos = Vector3.Lerp(lastPosition, transform.position, (float)j / (float)numSteps);
                var dir = Vector3.Slerp(lastDirection, curDirection, (float)j / (float)numSteps);
                if (drill)
                    PaintRayAll(pos, dir);
                else
                    PaintRay(pos, dir);
            }
        } else {
            if (drill)
                PaintRayAll(transform.position, curDirection);
            else
                PaintRay(transform.position, curDirection);
        }
    }

    void PaintRayAll(Vector3 pos, Vector3 dir)
    {
        // Paint all places where this ray hits paintable objects
        
        RaycastHit hit;
        List<Vector3> hitList = new List<Vector3>();
        GameObject gObject = null;

        // Forward pass: record hits (ray enters surface) and paint them
        while (Physics.Raycast(pos, dir, out hit, maxDist, layerMask))
        {
            GameObject g = hit.transform.gameObject;
            gObject = g;
            pt.PaintUV(g, hit.textureCoord);
            hitList.Add(hit.point);
            pos = hit.point + 0.001f * dir; // move slightly forward of latest hit
        }

        // Backward pass: Detect surface exits and paint them
        if (hitList.Count != 0)
        {
            Vector3 backHitStart = hitList[hitList.Count - 1] + (maxForward * dir);
            hitList.Add(backHitStart);
            hitList.Reverse();
            for (int i = 0; i < hitList.Count; i++)
            {
                if (Physics.Raycast(hitList[i], (-1 * dir), out hit, maxDist, layerMask))
                {
                    pt.PaintUV(gObject, hit.textureCoord);
                }
            }
        }
    }

    void PaintRayFirst(Vector3 pos, Vector3 dir)
    {
        // Paint only the first place where this ray touches a paintable object
        RaycastHit hit;

        // Cast a ray in direction "up", determine what paintable is first hit.
        if (Physics.Raycast(pos, dir, out hit, maxDist, layerMask))
        {
            GameObject g = hit.transform.gameObject;
            pt.PaintUV(g, hit.textureCoord);
        }
    }



    void PaintRay(Vector3 pos, Vector3 dir) {
        if (drill) {
            PaintRayAll(pos,dir);
        } else {
            PaintRayFirst(pos,dir);
        }
    }

    public void setColor()
    {
        RaycastHit hit;
        var raydir = transform.TransformDirection(Vector3.up);
        if (Physics.Raycast(transform.position, raydir, out hit, maxDist, colorMask))
        {

            GameObject g = hit.transform.gameObject;

            Color c = g.GetComponent<Renderer>().material.color;

            pt.SetDrawingColor(c);

        }
        else if (Physics.Raycast(transform.position, raydir, out hit, maxDist, textureColorMask))
        {
            // HAVE to use the texture as a 2D texture for better normalized (u,v) coordinate of texture
            Texture2D tex = hit.transform.GetComponent<MeshRenderer>().material.mainTexture as Texture2D;
            Vector2 uvCoord = hit.textureCoord;

            // normalizing coordinates to the texture
            uvCoord.x *= tex.width;
            uvCoord.y *= tex.height;

            Color c = tex.GetPixel((int)uvCoord.x, (int)uvCoord.y);

            pt.SetDrawingColor(c);
        }
        else if (Physics.Raycast(transform.position, raydir, out hit, maxDist, invisibleMask))
        {
            Color c = Color.clear;

            pt.SetDrawingColor(c);
        }
        else if (Physics.Raycast(transform.position, raydir, out hit, maxDist, drillMask))
        {
            if (drill)
            {
                Debug.Log("Drillin");
                drill = false;

            }
            else
            {
                Debug.Log("Stopped Drilling");
                drill = true;
            }
        }

    }

}

