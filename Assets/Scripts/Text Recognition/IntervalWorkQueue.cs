// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class IntervalWorkQueue : MonoBehaviour
{
    [Tooltip("The interval (sec) on which to check queued speech.")]
    [SerializeField]
    private float queueInterval = 0.25f;

    public enum WorkState
    {
        Idle,
        Starting,
        PollingForCompletion
    }
    public IntervalWorkQueue()
    {
        this.workState = WorkState.Idle;
        this.queueEntries = new Queue<object>();
    }
    public void AddWorkItem(object workItem)
    {
        this.queueEntries.Enqueue(workItem);
    }
    public void Start()
    {
        base.InvokeRepeating("ProcessQueue", queueInterval, queueInterval);
    }
    void ProcessQueue()
    {
        if ((this.workState == WorkState.Starting) &&
          (this.WorkIsInProgress))
        {
            this.workState = WorkState.PollingForCompletion;
        }

        if ((this.workState == WorkState.PollingForCompletion) &&
          (!this.WorkIsInProgress))
        {
            this.workState = WorkState.Idle;
        }

        if ((this.workState == WorkState.Idle) &&
          (this.WorkedIsQueued))
        {
            this.workState = WorkState.Starting;
            object workEntry = this.queueEntries.Dequeue();
            this.DoWorkItem(workEntry);
        }
    }
    protected bool WorkedIsQueued
    {
        get
        {
            return (this.queueEntries.Count > 0);
        }
    }
    protected abstract void DoWorkItem(object item);
    protected abstract bool WorkIsInProgress { get; }
    WorkState workState;
    Queue<object> queueEntries;
}