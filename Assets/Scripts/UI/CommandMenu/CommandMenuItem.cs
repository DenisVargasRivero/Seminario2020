﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Core.Interaction;

public class CommandMenuItem : MonoBehaviour
{
    public event Action<OperationType, IInteractionComponent> OnOperationSelected = delegate { };

    [SerializeField]
    CommandMenuItemData data = null;

    public IInteractionComponent referenceComponent = null;

    //Componentes que queremos modificar.
    [SerializeField] TMP_Text _text = null;
    [SerializeField] Image _iconImageField = null;
    [SerializeField] Image _backGroundImage = null;
    public Sprite normalState;
    public Sprite onHover;
    public Sprite onPress;

    [SerializeField] Color _normalColor  = Color.white;
    [SerializeField] Color _hoverColor   = Color.white;
    [SerializeField] Color _pressedColor = Color.white;

    public CommandMenuItemData Data
    {
        get => data;
        set
        {
            //Cargamos y mostramos nuestra Data.
            if (value != null)
            {
                data = value;
                _text.SetText(data.CommandName);
                _iconImageField.sprite = data.Icon;
            }
            //_backGroundImage.color = _normalColor;
        }
    }

    public void OnMouseHoverStart()
    {
        //Si ponemos el mouse encima.
        //_backGroundImage.color = _hoverColor;
        _backGroundImage.sprite = onHover;
        _text.color = Color.white;
        
    }
    public void OnMOuseClickDown()
    {
        //Si hacemos clic.
        //_backGroundImage.color = _pressedColor;
        //Ejecutamos nuestro delegado.
        _backGroundImage.sprite = onPress;
        OnOperationSelected(data.Operation, referenceComponent);
    }
    public void OnMouseClickUp()
    {
        _backGroundImage.color = _hoverColor;
    }
    public void OnMouseHoverEnd()
    {
        //Si sacamos el mouse de encima.
        //_backGroundImage.color = _normalColor;
        _backGroundImage.sprite = normalState;
        _text.color = Color.black;
    }
}
