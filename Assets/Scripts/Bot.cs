using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Bot : MonoBehaviour
{
    NavMeshAgent agent;
    public GameObject target; //target sera el cop que lo pondremos en Unity

    /// <summary>
    /// los estados
    /// </summary>
    enum State 
    {
        Idle, Hide, Pursue, Evade, Wander, Complex
    }
    enum ComplexState 
    { 
        Wander, Pursue, Hide 
    }

    State state = State.Idle;
    ComplexState complexState = ComplexState.Wander;

    /// <summary>
    /// distancia tiene que huir el robber
    /// </summary>
    public float fleeDistance = 10f;

    /// <summary>
    /// los rangos: wanderRadius el radio del círculo; wanderDistance la distancia del NPC al centro del círculo
    /// wanderJitter para la variabilidad de la dirección y con worldCenter hacemos que vuelve si el wander se sale del NavMesh

    /// </summary>
    public float wanderRadius = 3f;      
    public float wanderDistance = 6f;    
    public float wanderJitter = 0.5f;   
    public Vector3 worldCenter = Vector3.zero; 

    /// <summary>
    /// le ponemos una pequeña distancia detrá del obstaculo que se esconda
    /// </summary>
    public float hideMargin = 2f;
    public bool useCleverHide = true;

    /// <summary>
    /// angulos de visión del Cop
    /// </summary>
    public float viewAngle = 60f;         
    public float viewDistance = 40f;

    /// <summary>
    /// hacemos que entre en rango un poco debajo de la distancia y que salga por encima de ests
    /// y si el cop esta más cerca, pasa a Evade an vez de HIdee
    /// un coolDown para que se lo pensar dos veces, unos segundos sin ser visto antes de volver a acercarse
    /// </summary>
    public float enterRange = 20f;
    public float exitRange = 25f;         
    public float panicDistance = 6f;
    public float calmDownTime = 4f;

    Vector3 lastTargetPos;
    Vector3 targetVelocity;
    /// <summary>
    /// para guardar en memoria la última vez que el Cop vio al Robber
    /// </summary>
    float lastSeenTime = -999f;           
    bool inRange = false;                 


    // Start is called before the first frame update
    void Start()
    {
        agent = this.GetComponent<NavMeshAgent>();

        if (target == null)
        {
            Drive cop = FindFirstObjectByType<Drive>();
            if (cop != null) target = cop.gameObject;
        }
        if (target != null) lastTargetPos = target.transform.position;

        wanderTarget = Vector3.forward * wanderRadius;
    }


    /// <summary>
    /// ponemos el destino en el NavMesh por si el punto cae fuera, lo lleva al punto más cercano.
    /// </summary>
    /// <param name="destination"></param>
    /// <returns></returns>
    bool GoTo(Vector3 destination)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(destination, out hit, 5f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            return Vector3.Distance(hit.position, destination) < 1f;
        }
        return false;
    }
    void Seek(Vector3 location)
    {
        GoTo(location);

    }
    //En seek ponemos como destino la posicion del target, en Flee (huir) ponemos como
    //destino la posicion del target en negativo para que vaya huyendo de esa posicion
    void Flee(Vector3 location)
    {
        Vector3 away = transform.position - location;
        away.y = 0;
        away = away.normalized;

        if (!GoTo(transform.position + away * fleeDistance))
        {
            // Hemos chocado con el borde del NavMesh: escapamos hacia un lado
            Vector3 side = Vector3.Cross(Vector3.up, away) * fleeDistance;
            if (!GoTo(transform.position + side))
                GoTo(transform.position - side);
        }
    }

    /// <summary>
    /// calculamos la posición futura del Cop: distancia / (velocidad del NPC + velocidad del Cop)
    /// </summary>
    /// <returns></returns>
    Vector3 PredictedTargetPosition()
    {
        float targetSpeed = targetVelocity.magnitude;
        Vector3 pos = target.transform.position;
        if (targetSpeed < 0.1f) return pos; // quieto: no hay nada que predecir

        float distance = Vector3.Distance(transform.position, pos);
        float lookAhead = distance / (agent.speed + targetSpeed);
        return pos + targetVelocity * lookAhead;
    }
    void Pursue()
    {
        Seek(PredictedTargetPosition());
    }

    /// <summary>
    /// Evade es practicamente igual a Pursue solo que se debe ir en la direccion contraria
    /// </summary>
    void Evade()
    {
        Flee(PredictedTargetPosition());
    }


    ///Para Wander necesitamos una var. que recordemos en cada llamada y por lo
    //tanto no puede ser local al metodo.
    Vector3 wanderTarget = Vector3.zero; //Se actualiza cada vez que el agente
										 //tiene un nuevo valor en su posicion
    void Wander()
    {
        wanderTarget += new Vector3(
            Random.Range(-1f, 1f) * wanderJitter,
            0,
            Random.Range(-1f, 1f) * wanderJitter);

        //proyectamos de nuevo sobre el círculo
        wanderTarget = wanderTarget.normalized * wanderRadius;

        //el círculo está delante del NPC a coordenadas locales -> mundo
        Vector3 targetLocal = wanderTarget + new Vector3(0, 0, wanderDistance);
        Vector3 targetWorld = transform.TransformPoint(targetLocal);

        if (!GoTo(targetWorld))
        {
            //acercamos al límite del mundo, giramos hacia el centro
            GoTo(worldCenter);
            wanderTarget = Vector3.forward * wanderRadius;
        }
    }

    void Hide()
    {
        GameObject[] hidingSpots = World.Instance.GetHidingSpots();
        if (hidingSpots.Length == 0) { Flee(target.transform.position); return; }

        float bestDist = Mathf.Infinity;
        Vector3 bestPos = transform.position;

        foreach (GameObject spot in hidingSpots)
        {
            Vector3 dir = spot.transform.position - target.transform.position;
            dir.y = 0;
            Vector3 hidePos = spot.transform.position + dir.normalized * 5f;

            float d = Vector3.Distance(transform.position, hidePos);
            if (d < bestDist) { bestDist = d; bestPos = hidePos; }
        }
        Seek(bestPos);
    }

    void CleverHide()
    {
        GameObject[] hidingSpots = World.Instance.GetHidingSpots();
        if (hidingSpots.Length == 0) { Flee(target.transform.position); return; }

        float bestDist = Mathf.Infinity;
        Vector3 bestPos = transform.position;

        foreach (GameObject spot in hidingSpots)
        {
            Vector3 hidePos = GetHidePosition(spot);
            float d = Vector3.Distance(transform.position, hidePos);
            if (d < bestDist) { bestDist = d; bestPos = hidePos; }
        }
        Seek(bestPos);
    }

    Vector3 GetHidePosition(GameObject spot)
    {
        Collider col = spot.GetComponentInChildren<Collider>();

        Vector3 center = (col != null) ? col.bounds.center : spot.transform.position;
        Vector3 hideDir = center - target.transform.position;
        hideDir.y = 0;
        hideDir = hideDir.normalized;

        float radius = 1f;
        if (col != null) radius = new Vector2(col.bounds.extents.x, col.bounds.extents.z).magnitude;

        //un punto seguro fuera del collider, en el lado opuesto al Cop
        Vector3 outside = center + hideDir * (radius + hideMargin);
        outside.y = center.y;

        if (col != null)
        {
            //con un raycast desde fuera hacia el objeto, donde choca es su cara trasera
            Ray back = new Ray(outside, -hideDir);
            RaycastHit info;
            if (col.Raycast(back, out info, (radius + hideMargin) * 2f))
                return info.point + hideDir * hideMargin;
        }
        return outside;
    }
    bool CanSeeTarget()
    {
        Vector3 dir = target.transform.position - transform.position;
        if (dir.magnitude > viewDistance) return false;

        RaycastHit hit;
        if (Physics.Raycast(transform.position, dir, out hit, viewDistance))
            return hit.transform == target.transform || hit.transform.IsChildOf(target.transform);
        return false;
    }

    //En el siguiente comportamiento el robber se acerca al cop mientras este no le mira pero, cuando esto sucede, el robber se esconde.
    //Tenemos que decidir cuando el cop ve al rubber, digamos que esto sucede si esta en su angulo de vision (p.e. 60�)
    bool TargetCanSeeMe()
    {
        Vector3 toMe = transform.position - target.transform.position;
        if (toMe.magnitude > viewDistance) return false;
        if (Vector3.Angle(target.transform.forward, toMe) > viewAngle) return false;

        RaycastHit hit;
        if (Physics.Raycast(target.transform.position, toMe, out hit, viewDistance))
            return hit.transform == transform || hit.transform.IsChildOf(transform);
        return false;
    }

    /// <summary>
    /// si el Cop esta cerca se entra en rango a enterRange y se sale a exitRange
    /// </summary>
    /// <returns></returns>
    bool IsTargetInRange()
    {
        float d = Vector3.Distance(transform.position, target.transform.position);
        if (!inRange && d < enterRange) inRange = true;
        else if (inRange && d > exitRange) inRange = false;
        return inRange;
    }

    /// <summary>
    /// compotamiento copmplejo del Robber
    /// </summary>
    void ComplexBehaviour()
    {
        bool near = IsTargetInRange();
        bool seen = TargetCanSeeMe();
        if (seen) lastSeenTime = Time.time; //memoria

        switch (complexState)
        {
            case ComplexState.Wander:
                Wander();
                if (near && CanSeeTarget())
                    complexState = seen ? ComplexState.Hide : ComplexState.Pursue;
                break;
            case ComplexState.Pursue:
                Pursue();
                if (!near) complexState = ComplexState.Wander;
                else if (seen) complexState = ComplexState.Hide;
                break;

            case ComplexState.Hide:
                //Evade + Hide: si el Cop está muy cerca huye, si no se esconde
                float d = Vector3.Distance(transform.position, target.transform.position);
                if (d < panicDistance) Evade();
                else CleverHide();

                //no vuelve a acercarse hasta llevar calmDownTime segundos sin ser visto
                if (Time.time - lastSeenTime > calmDownTime)
                    complexState = near ? ComplexState.Pursue : ComplexState.Wander;
                break;
        }
    }
    void ChangeState(State newState)
    {
        state = newState;

        if (newState == State.Wander) wanderTarget = Vector3.forward * wanderRadius;
        if (newState == State.Idle) agent.ResetPath();
        if (newState == State.Complex)
        {
            complexState = ComplexState.Wander;
            lastSeenTime = -999f;
            inRange = false;
        }
        Debug.Log(name + " -> " + newState);
    }

    void HandleInput()
    {
        if(Input.GetKeyDown(KeyCode.H))
        { 
             ChangeState(State.Hide);

        }
        if(Input.GetKeyDown(KeyCode.P))
        {
            ChangeState(State.Pursue);
        }
        if (Input.GetKeyDown(KeyCode.E)) 
        { 
            ChangeState(State.Evade);
        }
        if (Input.GetKeyDown(KeyCode.R)) 
        { 
            ChangeState(State.Wander);
        }
        if (Input.GetKeyDown(KeyCode.C)) 
        {
            ChangeState(State.Complex);
        }
        if (Input.GetKeyDown(KeyCode.I)) 
        {
            ChangeState(State.Idle);
        }
    }

    //la velocidad del Cop medida entre frames 
    void UpdateTargetVelocity()
    {
        if (Time.deltaTime <= 0f) return;
        Vector3 v = (target.transform.position - lastTargetPos) / Time.deltaTime;
        targetVelocity = Vector3.Lerp(targetVelocity, v, 0.3f);
        lastTargetPos = target.transform.position;
    }


    // Update is called once per frame
    void Update()
    {
        if (target == null) return;

        UpdateTargetVelocity();
        HandleInput();

        switch (state)
        {
            case State.Idle: 
                break;
            case State.Hide:
                if (useCleverHide)
                {
                    CleverHide();
                }
                else 
                {
                    Hide();
                }
                break;
            case State.Pursue: 
                Pursue();
                break;
            case State.Evade: 
                Evade();
                break;
            case State.Wander:
                Wander();
                break;
            case State.Complex: 
                ComplexBehaviour();
                break;
        }
    }


    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.TransformPoint(Vector3.forward * wanderDistance), wanderRadius);
        Gizmos.DrawSphere(transform.TransformPoint(wanderTarget + Vector3.forward * wanderDistance), 0.3f);
    }
}