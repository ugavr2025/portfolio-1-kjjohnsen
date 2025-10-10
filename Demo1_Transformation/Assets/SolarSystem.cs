using UnityEngine;
using UnityEngine.InputSystem;

public class SolarSystem : MonoBehaviour
{

	private float earthRevolutionPeriod = 365.25f;
	private float moonRevolutionPeriod = 27.3f;
	private float distanceEarthSun = 93.473f; //million miles
    private float distanceEarthMoon = .2389f; //million miles
    private float relativeEarthSunSize = 1 / 103f;
    private float relativeEarthMoonSize = 1 / 3.7f;
	private float sunDiameter = .86537f;
    private float sunRotationPeriod = 25.05f;
	public float earthMoonDistanceScale = 1f;
    public float earthSunDistanceScale = 1f;
    public float sunScale = 1f;
    public float earthScale = 1f;
    public float moonScale = 1f;
    public float solarSpeedScale = 1f;

	public Transform earth;
    public Transform earthTilted;
    public Transform sun; //do we really need this?
    public Transform moon;
    public Transform solarLight;
    public Transform viewCamera;

    private float viewCameraDistance = 100;
	private float viewCameraYaw;
	private float viewCameraPitch;

	public float solarTime = 0; //let's say days
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

		viewCameraDistance -= Mouse.current.scroll.y.ReadValue()*2;
		if (Mouse.current.leftButton.isPressed)
        {
            
            viewCameraYaw += Mouse.current.delta.x.ReadValue() / 10;
            viewCameraPitch += Mouse.current.delta.y.ReadValue() / 10;
        }
        viewCameraPitch = Mathf.Clamp(viewCameraPitch, -89.9f, 89.9f);
		solarTime = solarTime + Time.deltaTime * solarSpeedScale;
        sun.localScale = Vector3.one* sunDiameter*sunScale;
        sun.localRotation = Quaternion.Euler(0, -360*solarTime/sunRotationPeriod, 0);
        //first make the earth rotate around the sun every 365 days
        var theta = 2*Mathf.PI * solarTime / earthRevolutionPeriod; 
		earth.position = new Vector3(Mathf.Cos(theta),0,Mathf.Sin(theta))*distanceEarthSun * earthSunDistanceScale;
        earth.localScale = Vector3.one * sunDiameter * relativeEarthSunSize * earthScale;
        //now, make the earth rotate on its axis every 24 hours.
        //The problem is that it's also revolving around the sun, so at half way, night and day are flipped
        //we can rotate a bit faster to correct (adding in the solar revolution)
        earthTilted.localRotation = Quaternion.Euler(0, -(360*solarTime + 360*(solarTime/ earthRevolutionPeriod)), 0);

        //now deal with the moon
        //The vector from the earth to the moon is rotating
        var moonTheta = 2 * Mathf.PI * solarTime / moonRevolutionPeriod;
        var earthToMoon = new Vector3(Mathf.Cos(moonTheta),0,Mathf.Sin(moonTheta))*distanceEarthMoon * earthMoonDistanceScale;
        moon.position = earth.position + earthToMoon;
        moon.localScale = Vector3.one * sunDiameter * relativeEarthSunSize * relativeEarthMoonSize * moonScale;
        moon.LookAt(earth); //moon always faces the earth (roughly)

        solarLight.LookAt(earth); //sunlight always shines towards earth (roughly)

        Vector3 viewCamVec = new Vector3(0, 0, viewCameraDistance);
		viewCamera.position = Quaternion.Euler(0, viewCameraYaw, 0) * Quaternion.Euler(viewCameraPitch, 0, 0) * viewCamVec;
        viewCamera.LookAt(sun);
	    
        
    }
}
