# IA Behaviours: un ladrón con NavMesh y máquinas de estados

Trabajo de Inteligencia Artificial.

## 1. De qué va la práctica

La escena tiene un suelo con NavMesh, un árbol, varios obstáculos que son unos cubos y dos cápsulas: un policía "Cop", que se controla en primera persona con WASD, y un ladrón "Robber", que es el NPC. El objetivo es dar al ladrón movimiento autónomo mediante behaviours, que se activan con el teclado.

Tecla	   | Comportamiento
P	Pursue:  hace que el Robber perdiga al Cop
E	Evade:   huye de donde estará el Cop
R	Wander:  deambula de forma controlada
H	Hide:    se esconde detrás de un obstáculo
C	Complex: combina todo lo anterior
I	Idle:    se queda parado

[Vista general de la escena con el Cop y los Robber]
<img width="1867" height="813" alt="image" src="https://github.com/user-attachments/assets/c2e53d54-1d39-4110-b387-eb54ff7bd285" />

## 2. Máquina de estados

La decisión de qué hacer está separada de la ejecución de cómo hacerlo. En el script "Bot" tiene una máquina de estados con seis estados, y en cada frame ejecuta el comportamiento del estado actual. Las teclas cambian de estado desde cualquier otro.

### FSM principal

El estado Complex contiene su propia sub-máquina, que lo explica en el apartado 4.

## 3. Los comportamientos

Todos usan un NavMeshAgent, desde el Environment con un NavMeshSurface dentro de el con los obstáculos con un tag de "hide" y con un NavMeshObstacle así que el Robber respeta los límites del mundo y los obstáculos.

- Pursue y Evade:
Pursue no va hacia donde está el Cop, sino hacia donde estará. Predice su posición futura con la fórmula distancia / (velocidad del NPC + velocidad del cop).
Evade usa la misma predicción, pero en lugar de ir hacia ese punto, huye de él. La velocidad del Cop lo he medido entre frames, porque Drive.cs solo expone el desplazamiento por frame.

float lookAhead = distance / (agent.speed + targetSpeed);
Vector3 predicted = target.position + targetVelocity * lookAhead;

  void UpdateTargetVelocity()
  {
      if (Time.deltaTime <= 0f) return;
      Vector3 v = (target.transform.position - lastTargetPos) / Time.deltaTime;
      targetVelocity = Vector3.Lerp(targetVelocity, v, 0.3f);
      lastTargetPos = target.transform.position;
  }
  
[GIF: Pursue interceptando al Cop] 
<img width="1920" height="1080" alt="ezgif com-optimize (1)" src="https://github.com/user-attachments/assets/89aad23a-747e-45ac-b6ce-41d789744758" />

[GIF: Evade]
<img width="1920" height="1080" alt="ezgif com-optimize (2)" src="https://github.com/user-attachments/assets/ce0e87c1-b67a-47bf-863c-b861ec292b9a" />

- Wander: El movimiento es aleatorio, pero no tanto como para cambiar de dirección a cada momento. Se usa un círculo delante del NPC con tres parámetros:

wanderDistance: distancia del NPC al centro del círculo.
wanderRadius: radio del círculo.
wanderJitter: cuánto se mueve el punto objetivo sobre el círculo en cada frame.

wanderTarget += new Vector3(Random.Range(-1f, 1f) * wanderJitter, 0,
                            Random.Range(-1f, 1f) * wanderJitter);
wanderTarget = wanderTarget.normalized * wanderRadius;

Si el punto cae fuera del NavMesh, el Robber se dirige hacia el centro del mundo para no quedarse pegado al borde.

[GIF: Wander]
<img width="1920" height="1080" alt="ezgif com-optimize (3)" src="https://github.com/user-attachments/assets/c3cc928f-a086-4ff7-b32f-dc77edb0100d" />
<img width="1920" height="1080" alt="ezgif com-optimize (5)" src="https://github.com/user-attachments/assets/ad88bdf3-1bbc-4ebf-ab43-ea5b4a06ad51" />


- Hide: La primera versión "Hide" coloca al ladrón a una distancia fija detrás del obstáculo. Pero eso falla con objetos de distinto tamaño, así que la versión final es CleverHide.

Para cada obstáculo con el tag "hide", calcula la dirección Cop -> obstáculo. Lanza un rayo desde el lado opuesto hacia el collider del objeto para encontrar su cara trasera.
Se coloca justo detrás hideMargin y elige el escondite más cercano a él, por lo que funciona igual con una caja pequeña, una grande o el árbol.

[GIF: Hide con obstáculos]
<img width="1920" height="1080" alt="Hide" src="https://github.com/user-attachments/assets/1c8de161-c70d-455d-b7a0-ebca97a96792" />


## 4. Comportamiento complejo (tecla C)

El ladrón se acerca al policía cuando este no lo mira, pero huye y se esconde cuando lo ve. Si están muy lejos, simplemente deambula. Es una sub-máquina de tres estados:

Sub-máquina de Complex
<img width="1089" height="610" alt="image" src="https://github.com/user-attachments/assets/fe7250ff-62c0-40da-bb3d-28b03299ab1e" />
<img width="1081" height="608" alt="image" src="https://github.com/user-attachments/assets/a28a3a52-e555-43ef-9588-e88fa0caba1f" />

### Percepción:

CanSeeTarget(): línea de visión del robber hacia el cop (con raycast).
TargetCanSeeMe(): el Robber está dentro del ángulo de visión del Cop y no hay nada en medio.
IsTargetInRange(): Cop cerca o lejos, con histéresis.

Un ladrón que cambia de Pursue a Flee en cada frame parece nervioso y poco creíble. Para evitarlo he combinado dos soluciones:

- Histéresis: entra en rango a 20m y sale a 25 m, no al mismo valor.
- Memoria: tras ser visto, no vuelve a acercarse hasta pasados calmDownTime segundos sin ser visto. Es lo que el enunciado llama "pensárselo dos veces".

Dentro de Hide, si el Cop está muy cerca panicDistance, el Robber hace Evade. Si no, se esconde con CleverHide.

[GIF: Complex: wander, pursue, hide, espera, pursue]
<img width="1920" height="1080" alt="ezgif com-cut" src="https://github.com/user-attachments/assets/c3f97e00-ac92-4b7e-b396-0ae927d5d322" />

## 5. Varios NPCs

Duplique al Robber en varios, donde cada instancia decide por sí misma, así que si están cerca, se esconden en el mismo sitio, pero si estaban lejos unos de otros, cada uno elige el escondite más cercano a su posición.

## 6. Problemas y aprendizajes
Nerviosismo en Wander: el jitter se aplica en cada frame, así que el resultado depende de los FPS. Se arregla ajustando wanderJitter, wanderDistance y la velocidad angular del agente.
Bordes del NavMesh: Flee y Wander pueden mandar al agente fuera del mapa. Hay que sacar el punto más cercano del NavMesh y, si se choca con el borde, escapar hacia un lado o hacia el centro.
Obstáculos: tienen que estar bakeados en el NavMesh y con un NavMeshObstacle llevando el tag hide y un collider.

Para Descargar el ejecutable desde aquí: [(https://github.com/Anpije/AI_Behaviour/releases/latest)]

Repositorio: [https://github.com/Anpije/IA_Behaviour.git]
