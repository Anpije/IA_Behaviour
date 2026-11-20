# IA_Behaviour

## Práctica de Inteligencia Artificial en Unity 6000.3.2f1:
Un NPC ladrón "Robber" que se mueve de forma autónoma con NavMeshAgent mediante distintos behaviours, con una máquina de estados. El policía "Cop" se controla en primera persona con WASD.

## Controles
- Tecla        |	Acción
- WASD/flechas	  Mover al Cop
- P	Pursue:       el ladrón intercepta al Cop
- E	Evade:        el ladrón huye de la posición futura del Cop
- R	Wander:       deambula de forma controlada y no de manera aleatoria del todo
- H	Hide:         se esconde detrás de obstáculos
- C	Complex:      se acerca si el Cop no lo ve, se esconde si lo ve
- I	Idle:         se queda parado

## Máquina de estados

Cada tecla cambia el estado del Bot:
<img width="1089" height="610" alt="image" src="https://github.com/user-attachments/assets/3181eb12-dd67-4537-865a-9ea9465a4785" />

El estado Complex contiene una sub-máquina:
<img width="1081" height="608" alt="image" src="https://github.com/user-attachments/assets/b4ebd911-9344-4078-b7ce-ecd86e1e423a" />

## Behaviours

- Pursue / Evade: predicen la posición del Cop con (distancia / (velocidad NPC + velocidad cop)).
- Wander: círculo delante del NPC dibujado con un Gizmo (wanderDistance, wanderRadius, wanderJitter).
- Hide "CleverHide": usa el collider del obstáculo para colocarse en su cara trasera, funciona con cualquier tamaño.
- Complex: combina los anteriores con percepción (CanSeeTarget, TargetCanSeeMe, IsTargetInRange), histéresis en la distancia y memoria para no cambiar de estado a cada frame.
  
## Parámetros ajustables 

- Parámetros                                      |  Para qué sirve
- wanderRadius, wanderDistance, wanderJitter:	       Forma y suavidad del Wander
- fleeDistance:	                                     Distancia del destino al huir
- hideMargin:	                                       Distancia extra detrás del obstáculo
- viewAngle, viewDistance:	                         Visión del Cop
- enterRange, exitRange:	                           Histéresis de Cop cerca
- panicDistance:	                                   Por debajo, el ladrón huye en vez de esconderse
- calmDownTime:	                                     Segundos sin ser visto antes de volver a acercarse
- useCleverHide:	                                   Alterna entre Hide básico y CleverHide

## Varios NPCs

Basta con duplicar el Prefab del "Robber" en la escena, ya que cada instancia decide por sí misma y elige el escondite más cercano a su posición.

## Trabajo realizado por 

- Angeleau Pierre Jeudi ---> Enlace al Blog:
- Ejecutable: Descargar la build: 
