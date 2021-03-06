﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core.DamageSystem;
using System;
using IA.PathFinding;
using Core.Interaction;
using Core.InventorySystem;
using Core.SaveSystem;
using System.Linq;

[RequireComponent(typeof(PathFindSolver), typeof(MouseContextTracker))]
public class Controller : MonoBehaviour, IDamageable<Damage, HitResult>, ILivingEntity
{
    //Stats.
    public int Health = 100;

    public event Action OnPlayerDied = delegate { };
    public event Action OnMovementChange = delegate { };
    public event Action OnInputLocked = delegate { };
    public event Action<Collider> OnEntityDead = delegate { };

    public Transform manitodumacaco;
    public ParticleSystem BloodStain;

    public float moveSpeed = 6;
    public Transform MouseDebug;

    Queue<IQueryComand> comandos = new Queue<IQueryComand>();

    Inventory _inventory = new Inventory();
    public Inventory Inventory
    {
        get
        {
            if (_inventory == null)
                _inventory = new Inventory();

            return _inventory;
        }
        set =>_inventory = value;
    }

    [SerializeField] bool _input = true;
    bool PlayerInputEnabled
    {
        get => _input;
        set
        {
            _input = value;
            if (!value)
                OnInputLocked();
        }
    }
    bool ClonInputEnabled = true;
    Vector3 velocity;

    public void SubscribeToLifeCicleDependency(Action<Collider> OnEntityDead)
    {
        this.OnEntityDead += OnEntityDead;
    }
    public void UnsuscribeToLifeCicleDependency(Action<Collider> OnEntityDead)
    {
        this.OnEntityDead -= OnEntityDead;
    }

    //================================= Save System ========================================

    public PlayerData getCurrentPlayerData()
    {
        var equiped = Inventory.equiped;

        var data = new PlayerData()
        {
            position = transform.position,
            rotacion = transform.rotation,
            EquipedItem = equiped != null ? Inventory.equiped.ID : ItemID.nulo,
            itemScale = equiped != null ? equiped.transform.localScale : Vector3.zero,
            itemRotation = equiped != null ? equiped.transform.rotation : Quaternion.identity,
            maxItemsSlots = Inventory.maxItemsSlots,
            inventory = Inventory.slots.Select(x => x.ID)
                                         .ToList()
        };

        if (equiped != null && equiped.ID == ItemID.Antorcha && ((Torch)equiped).isBurning)
            data.itemIsActive = true;

        return data;
    }
    public void LoadPlayerCheckpoint(PlayerData data)
    {
        transform.position = data.position;
        transform.rotation = data.rotacion;

        if (Inventory.equiped != null)
            Destroy(Inventory.equiped.gameObject);
        Inventory = new Inventory();
        if (data.EquipedItem == ItemID.nulo)
            Inventory.equiped = null;
        else
        {
            var instance = Instantiate(ItemDataBase.getRandomItemPrefab(data.EquipedItem));
            instance.transform.localScale = data.itemScale;
            instance.transform.rotation = data.rotacion;

            if (data.EquipedItem == ItemID.Antorcha && data.itemIsActive)
            {
                var torch = instance.GetComponent<Torch>();
                torch.isBurning = true;
            }

            AttachItemToHand(instance.GetComponent<Item>());
        }
        Inventory.maxItemsSlots = data.maxItemsSlots;
        Inventory.slots = new List<Item>();

        foreach (var item in data.inventory) //Reconstruimos el inventario.
        {
            var itemdata = ItemDataBase.getItemData(item);
            var toAddItem = ItemDataBase.getRandomItemPrefab(item).GetComponent<Item>();

            Inventory.slots.Add(toAddItem);
        }

        _a_GetSmashed = false;
        _a_GetStunned = false;
        _a_Dead = false;
        _a_Walking = false;
        Health = 100;
        PlayerInputEnabled = true;
        BloodStain.Clear();
        BloodStain.Stop();

        //_rb.useGravity = false;
        _rb.velocity = Vector3.zero;
        _rb.useGravity = false;
        _rb.isKinematic = true;
        _hitbox.enabled = true;
        _hitbox.isTrigger = false;
        OnEntityDead = delegate { };
    }

    //======================================================================================

    #region Componentes
    [Header("Line of sight Options")]
    [SerializeField] bool _useCustomLOSTarget = false;
    [SerializeField] Transform _lineOfSightTarget = null;

    [Header("Componentes adicionales")]
    [SerializeField] Collider _hitbox = null;
    [SerializeField] CommandMenu _MultiCommandMenu = null;
    [SerializeField] InspectionMenu _inspectionMenu = null;
    Rigidbody _rb;
    Camera _viewCamera;
    CanvasController _canvasController = null;
    MouseView _mv;
    MouseContextTracker _mtracker;
    PathFindSolver _solver;
    MouseContext _mouseContext;
    TrowManagement _tm;
    bool _Aiming;
    Transform throwTarget;
    #endregion
    #region Clon
    [Header("Clon")]
    public ClonBehaviour Clon = null;
    [SerializeField] float _clonCooldown = 4f;
    bool _canCastAClon = true;
    #endregion
    #region Animaciones
    Animator _anims;
    int[] animHash = new int[4];
    public float TRWRange;

    bool _a_Walking
    {
        get => _anims.GetBool(animHash[0]);
        set => _anims.SetBool(animHash[0], value);
    }
    bool _a_Crouching
    {
        get => _anims.GetBool(animHash[1]);
        set => _anims.SetBool(animHash[1], value);
    }
    bool _a_Activate
    {
        get => _anims.GetBool(animHash[2]);
        set => _anims.SetBool(animHash[2], value);
    }
    bool _a_Dead
    {
        get => _anims.GetBool(animHash[3]);
        set => _anims.SetBool(animHash[3], value);
    }
    bool _a_Ignite
    {
        get => _anims.GetBool(animHash[4]);
        set => _anims.SetBool(animHash[4], value);
    }
    bool _a_Clon
    {
        get => _anims.GetBool(animHash[5]);
        set => _anims.SetBool(animHash[5], value);
    }
    bool _a_ThrowRock
    {
        get => _anims.GetBool(animHash[6]);
        set => _anims.SetBool(animHash[6], value);
    }
    bool _a_GetStunned
    {
        get => _anims.GetBool(animHash[7]);
        set => _anims.SetBool(animHash[7], value);
    }
    int _a_KillingMethodID
    {
        get => _anims.GetInteger(animHash[8]);
        set => _anims.SetInteger(animHash[8], value);
    }
    bool _a_GetSmashed
    {
        get => _anims.GetBool(animHash[9]);
        set => _anims.SetBool(animHash[9], value);
    }
    bool _a_Grabing
    {
        get => _anims.GetBool(animHash[10]);
        set => _anims.SetBool(animHash[10], value);
    }
    int _a_ActivationType
    {
        get => _anims.GetInteger(animHash[11]);
        set => _anims.SetInteger(animHash[11], value);
    }
    #endregion

    public Vector3 getLineOfSightTargetPosition()
    {
        if (_useCustomLOSTarget && _lineOfSightTarget != null)
            return _lineOfSightTarget.position;

        return transform.position;
    }

    //================================= UnityEngine ========================================

    private void Awake()
    {
        //Componentes.
        _rb = GetComponent<Rigidbody>();
        _viewCamera = Camera.main;
        _canvasController = FindObjectOfType<CanvasController>();
        OnPlayerDied += _canvasController.DisplayLoose;
        _mv = GetComponent<MouseView>();
        _mtracker = GetComponent<MouseContextTracker>();
        _solver = GetComponent<PathFindSolver>();
        _tm = GetComponent<TrowManagement>();
        _inspectionMenu = FindObjectOfType<InspectionMenu>();

        if (_inspectionMenu)
            _inspectionMenu.OnSetInspection += (value) => { PlayerInputEnabled = !value; };

        if (!_hitbox)
            _hitbox = GetComponent<Collider>();

        if (_MultiCommandMenu)
            _MultiCommandMenu.commandCallback = QuerySelectedOperation;

        if (_solver.Origin == null)
        {
            var closerNode = _solver.getCloserNode(transform.position);
            transform.position = closerNode.transform.position;
            _solver.SetOrigin(closerNode);
        }

        //Clon.
        if (Clon != null)
        {
            Clon.Awake();
            Clon.RegisterRecastDependency(ClonDeactivate);
        }

        //Animaciones.
        _anims = GetComponent<Animator>();
        animHash = new int[12];
        var animparams = _anims.parameters;
        for (int i = 0; i < animHash.Length; i++)
            animHash[i] = animparams[i].nameHash;
    }
    void Update()
    {
        //Start Pause
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            //print("Pauso la Wea");
            Level.TooglePauseGame();
            _canvasController.setPauseMenu(Level.isPaused);
            return;
        }

        if (Level.isPaused)
            return;

        _mouseContext = _mtracker.GetCurrentMouseContext(_inventory);
        if (!_Aiming)
        {
            if (_mouseContext.interactuableHitted)
                _mtracker.ChangeCursorView(2);
            else
                _mtracker.ChangeCursorView(1);
        }
        else _mtracker.ChangeCursorView(3);

        var equiped = _inventory.equiped;
        _canvasController.DisplayPlayerUI(true, equiped != null && equiped.data.isThroweable);

        #region Input
        if (PlayerInputEnabled)
        {
            if (_Aiming)
            {
                if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Alpha2))
                {
                    //print("Cancelo el tiro");
                    _Aiming = false;
                    return;
                }

                //Confirmar el tiro, clic izquierdo
                if (Input.GetMouseButtonDown(0))
                {
                    //print("Confirmo el tiro");
                    _Aiming = false;
                    _mouseContext = _mtracker.GetCurrentMouseContext();
                    if (_mouseContext.closerNode)
                    {
                        //En vez de ejecutarlo directamente. Añadimos un TrowCommand.
                        var command = new cmd_ThrowEquipment(
                            manitodumacaco,
                            _mouseContext.closerNode,
                            transform,
                            1f, 
                            _tm, 
                            ReleaseEquipedItemFromHand,
                            () =>
                            {
                                _a_ThrowRock = true;
                            }
                        );
                        comandos.Enqueue(command);
                    }

                    return;
                }
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.Alpha2) && Inventory.equiped != null)
                {
                    _Aiming = true;
                    return;
                }

                // MouseClic Derecho.
                if (Input.GetMouseButtonDown(1))
                {
                    //MouseContext _mouseContext = _mtracker.GetCurrentMouseContext();//Obtengo el contexto del Mouse.
                    if (!_mouseContext.validHit) return; //Si no hay hit Válido.

                    if (_mouseContext.interactuableHitted)
                    {
                        _MultiCommandMenu.SetCommandMenu(Input.mousePosition, _mouseContext.InteractionHandler, _inventory, QuerySelectedOperation);
                        return;
                    }

                    if (Input.GetKey(KeyCode.LeftControl) && Clon.IsActive)
                        Clon.SetMovementDestinyPosition(_mouseContext.closerNode);
                    else
                    {
                        Node targetNode = _mouseContext.closerNode;
                        if (targetNode == null)
                            return;

                        if (Input.GetKey(KeyCode.LeftShift)) //Si presiono shift, muestro donde estoy presionando de forma aditiva.
                        {
                            AddMovementCommand(targetNode);
                            _mv.SetMousePositionAditive(_mouseContext.closerNode.transform.position);
                        }
                        else //Si no presiono nada, es una acción normal, sobreescribimos todos los comandos!.
                        {
                            CancelAllCommands();
                            AddMovementCommand(targetNode);
                            _mv.SetMousePosition(_mouseContext.closerNode.transform.position);
                        }
                    }
                }
            }
        }
        #endregion
        #region Clon Input
        if (ClonInputEnabled)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) && !Clon.IsActive)
            {
                comandos.Clear();
                _a_Clon = true;
                PlayerInputEnabled = false;
                ClonSpawn();
            }
        }   
        #endregion

        if (comandos.Count > 0)
        {
            //print($"Comandos activados: {comandos.Count}");
            IQueryComand current = comandos.Peek();
            if (!current.isReady)
                current.SetUp();
            current.UpdateCommand();
        }
    }

    //================================= Damage System ======================================

    public bool IsAlive => Health > 0;

    public Damage GetDamageStats()
    {
        return new Damage()
        {
            Ammount = 10f,
            instaKill = false,
            criticalMultiplier = 2,
            type = DamageType.piercing
        };
    }
    public HitResult GetHit(Damage damage)
    {
        HitResult result = new HitResult() { conected = true, fatalDamage = true };
        if (IsAlive && damage.instaKill)
            Die(damage.KillAnimationType);
        return result;
    }
    public void FeedDamageResult(HitResult result) { }
    public void GetStun(Vector3 AgressorPosition, int PosibleKillingMethod)
    {
        Vector3 DirToAgressor = (AgressorPosition - transform.position).normalized;
        transform.forward = DirToAgressor;

        _a_Walking = false;
        _a_KillingMethodID = PosibleKillingMethod;
        _a_GetStunned = true;

        PlayerInputEnabled = false;
        comandos.Clear();
    }

    //================================== Player Controller =================================

    #region Clon
    public void ClonSpawn()
    {
        if (!Clon.IsActive && _canCastAClon)
        {
            Node position = _solver.getCloserNode(transform.position + transform.forward * 1.5f);
            Clon.InvokeClon(position, -transform.forward);
            _canCastAClon = false;
        }
    }
    public void finishClon()
    {
        _a_Clon = false;
        PlayerInputEnabled = true;
        _canCastAClon = true;
    }
    public void ClonDeactivate()
    {
        StartCoroutine(clonCoolDown());
    }
    IEnumerator clonCoolDown()
    {
        _canCastAClon = false;
        yield return new WaitForSeconds(_clonCooldown);
        _canCastAClon = true;
    } 
    #endregion

    public void AttachItemToHand(Item item)
    {
        //print($"{ gameObject.name} ha pikeado un item. {item.name} se attachea a la mano.");
        //Presuponemos que el objeto es troweable.
        //Emparentamos el item al transform correspondiente.
        if (item != null)
        {
            item.SetOwner(_hitbox);
            item.SetPhysicsProperties(false, Vector3.zero);
            item.transform.SetParent(manitodumacaco);
            item.transform.localPosition = Vector3.zero;
            item.transform.up = manitodumacaco.up;
            item.ExecuteOperation(OperationType.Take);
            _inventory.EquipItem(item);
            //DisplayThrowUI(_inventory.equiped != null && _inventory.equiped.isThroweable);
        }
    }
    public Item ReleaseEquipedItemFromHand(bool setToDefaultPosition = false, params object[] options)
    {
        //print($"{ gameObject.name} soltará un item equipado. {_inventory.equiped.name} se attachea a la mano.");
        //Desemparentamos el item equipado de la mano.
        Item released = _inventory.UnEquipItem();

        if (setToDefaultPosition)
            released.ExecuteOperation(OperationType.Drop);
        else
            released.ExecuteOperation(OperationType.Drop, options[0]);
        released.SetPhysicsProperties(true, Vector3.zero);
        released.transform.SetParent(null);
        //DisplayThrowUI(_inventory.equiped != null && _inventory.equiped.isThroweable);
        return released;
    }

    public void FallInTrap()
    {
        PlayerInputEnabled = false;
        //playerMovementEnabled = false;
        comandos.Clear();

        _rb.useGravity = true;
        _rb.isKinematic = false;
        _hitbox.isTrigger = true;
    }
    public void PlayBlood()
    {
        BloodStain.Play();
    }
    /// <summary>
    /// Mueve este cuerpo en dirección a un nodo.
    /// </summary>
    /// <param name="targetNode">El nodo objetivo al que nos queremos mover.</param>
    /// <returns>Retorna verdadero, si hemos llegado al nodo objetivo.</returns>
    public bool MoveToTarget(Node targetNode)
    {
        if (targetNode == null)
            return false;
        Vector3 dirToTarget = (targetNode.transform.position - transform.position).normalized;
        transform.forward = dirToTarget.YComponent(0);
        transform.position += dirToTarget * moveSpeed * Time.deltaTime;
        return Vector3.Distance(transform.position, targetNode.transform.position) <= _solver.ProximityTreshold;
    }

    /// <summary>
    /// Añade un comando Move si hay un camino posible entre los 2 nodos dados por parámetros. Si no hay uno, el comando es ignorado.
    /// </summary>
    /// <param name="OriginNode">El nodo mas cercano al agente.</param>
    private void AddMovementCommand(Node TargetNode)
    {
        IQueryComand moveCommand = new cmd_Move
                            (
                                transform,
                                _solver,
                                TargetNode,
                                MoveToTarget,
                                (value) => { _a_Walking = value; },
                                () => { comandos.Dequeue(); },
                                OnMovementChange
                            );
        //Move Command se autovalida, si no hay camino posible, en SetUp ejecuta Dispose();
        comandos.Enqueue(moveCommand);
    }

    /// <summary>
    /// Añade un comando a la cola, desde el menú de comandos.
    /// </summary>
    /// <param name="target">El objetivo de dicha operación.</param>
    /// <param name="operation">El tipo de operacion que se desea ejecutar.</param>
    public void QuerySelectedOperation(OperationType operation, IInteractionComponent target)
    {
        //añado el comando correspondiente a la query.
        IQueryComand _toActivateCommand = null;
        switch (operation)
        {
            case OperationType.Ignite:
                _toActivateCommand = new cmd_Ignite(
                        target,
                        (AnimIndex, value) => 
                        {
                            if (AnimIndex == 0)
                                _a_Walking = value;
                            if (AnimIndex == 1)
                                _a_Ignite = value;
                        },
                        (AnimIndex) =>
                        {
                            if (AnimIndex == 0)
                                return _a_Walking;
                            if (AnimIndex == 1)
                                return _a_Ignite;
                            return false;
                        },
                        transform,
                        _solver,
                        MoveToTarget,
                        () => comandos.Dequeue(), //Dispose.
                        OnMovementChange //Recálculo de camino.
                    );
                break;

            case OperationType.Activate:
                _toActivateCommand = new Cmd_Activate(
                        target,    //Objetivo.
                        transform, //Referencia a mi cuerpo.
                        _solver,   //Referencia al Componente de Cálculo de camino.
                        (animIndex) =>
                        {
                            if (animIndex == 0)
                                return _a_Walking;
                            else
                                return _a_Activate;
                        }, //get value de animación.
                        (animIndex, value) => 
                        {
                            if (animIndex == 0)
                                _a_Walking = value;
                            if (animIndex == 1 || animIndex == 2)
                            {
                                _a_ActivationType = animIndex;
                                _a_Activate = value;
                            }
                        }, //Set value de animación.
                        MoveToTarget,//Función de movimiento.
                        () => comandos.Dequeue(), //Dispose.
                        OnMovementChange //Recálculo de camino.
                    );
                break;

            case OperationType.Equip:
                break;

            case OperationType.Take:
                if (_inventory.equiped == null)
                    _toActivateCommand = new cmd_Take(
                                (Item)target,
                                AttachItemToHand,
                                (animIndex, value) => 
                                {
                                    if (animIndex == 0)
                                        _a_Walking = value;
                                    if (animIndex == 1)
                                        _a_Grabing = value;
                                },
                                (animIndex) =>
                                {
                                    if (animIndex == 0)
                                        return _a_Walking;
                                    if (animIndex == 1)
                                        return _a_Grabing;
                                    return false;
                                },
                                transform,
                                _solver,
                                MoveToTarget,
                                () => comandos.Dequeue(), //Dispose.
                                OnMovementChange //Recálculo de camino.
                            );
                break;

            case OperationType.Exchange:
                _toActivateCommand = new cmd_Exchange(
                        (Item)target,
                        _inventory,
                        ReleaseEquipedItemFromHand, 
                        AttachItemToHand, 
                        (animIndex, value) => 
                        {
                            if (animIndex == 0)
                                _a_Walking = value;
                            if (animIndex == 1)
                                _a_Grabing = value;
                        },
                        (animIndex) =>
                        {
                            if (animIndex == 0)
                                return _a_Walking;
                            if (animIndex == 1)
                                return _a_Grabing;
                            return false;
                        },
                        transform,
                        _solver,
                        MoveToTarget,
                        () => comandos.Dequeue(), //Dispose.
                        OnMovementChange //Recálculo de camino.
                    );
                break;

            case OperationType.Combine:
                _toActivateCommand = new cmd_Combine(
                        (Item)target,
                        _inventory,
                        AttachItemToHand,
                        (animIndex, value) =>
                        {
                            if (animIndex == 0)
                                _a_Walking = value;
                            if (animIndex == 1)
                                _a_Grabing = value;
                        },
                        (animIndex) =>
                        {
                            if (animIndex == 0)
                                return _a_Walking;
                            if (animIndex == 1)
                                return _a_Grabing;
                            return false;
                        },
                        transform,
                        _solver,
                        MoveToTarget,
                        () => comandos.Dequeue(), //Dispose.
                        OnMovementChange //Recálculo de camino.
                    );
                break;

            case OperationType.lightOnTorch:
                if (_inventory.equiped.ID == ItemID.Antorcha)
                    _toActivateCommand = new cmd_LightOnTorch
                        (
                            target,
                            (Torch)_inventory.equiped,
                            (animIndex, value) => 
                            {
                                if (animIndex == 0)
                                    _a_Walking = value;
                                if (animIndex == 1)
                                    _a_Ignite = value;
                            },
                            (animIndex) =>
                            {
                                if (animIndex == 0)
                                    return _a_Walking;
                                if (animIndex == 1)
                                    return _a_Ignite;
                                return false;
                            },
                            transform,
                            _solver,
                            MoveToTarget,
                            () => comandos.Dequeue(), //Dispose.
                            OnMovementChange
                        );
                break;

            case OperationType.inspect:
                _toActivateCommand = new cmd_Inspect
                    (
                        target,
                        transform,
                        _solver,
                        MoveToTarget,
                        () => _a_Walking,
                        (value) => _a_Walking = value,
                        () => comandos.Dequeue(),
                        OnMovementChange,
                        _inspectionMenu.DisplayText
                    );
                break;

            default:
                break;
        }

        if (_toActivateCommand != null)
            comandos.Enqueue(_toActivateCommand);
    }

    //========================================================================================

    void CancelAllCommands()
    {
        foreach (var command in comandos)
        {
            command.Cancel();
        }
        comandos.Clear();
    }
    void Die(int KillingAnimType)
    {
        PlayerInputEnabled = false;
        Health = 0;

        _rb.useGravity = false;
        _rb.velocity = Vector3.zero;
        _a_KillingMethodID = KillingAnimType;
        _hitbox.enabled = false;

        if (KillingAnimType == 1)
            _a_GetSmashed = true;

        _a_Dead = true;

        comandos.Clear();

        OnEntityDead(_hitbox);
        OnPlayerDied();
    }

    //====================================== AnimEvents =======================================

    void AE_BlockInteractions()
    {
        PlayerInputEnabled = false;
        //print("Player input has been Loqued!");
        //Llamo a una función que cierre la ventana de interacción.
        if (_MultiCommandMenu.isActiveAndEnabled)
            _MultiCommandMenu.Close();
    }
    void AE_UnLockInteractions()
    {
        //print("Player input has been unloqued");
        PlayerInputEnabled = true;
    }

    void AE_PullLeverStarted()
    {
        //print($"Pull Lever Started:: {gameObject.name} ha iniciado la activación de una palanca");
    }
    void AE_PullLeverEnded()
    {
        _a_Activate = false;
        comandos.Dequeue().Execute();
    }
    void AE_PullGroundLeverStarted()
    {
        //print($"Pull Ground Lever Started:: {gameObject.name} ha iniciado la activación de una palanca");
    }
    void AE_PullGroundLeverEnded()
    {
        //print("========== Pull Ground Lever Ended ===============");
        _a_Activate = false;
        comandos.Dequeue().Execute();
    }
    void AE_Ignite_Start()
    {
        //PlayerInputEnabled = false;
    }
    void AE_Ignite_End()
    {
        _a_Ignite = false;

        comandos.Dequeue().Execute();
    }
    void AE_Throw_Start()
    {
        //Esto esta al pepe ahora mismo.
    }
    void AE_Throw_Excecute()
    {
        comandos.Peek().Execute();
    }
    void AE_TrowRock_Ended()
    {
        _a_ThrowRock = false;
        comandos.Dequeue();
    }
    void AE_Grab_Star()
    {
        //PlayerInputEnabled = false;
    }
    void AE_Grab_End()
    {
        _a_Grabing = false;

        comandos.Dequeue().Execute();
    }
}
