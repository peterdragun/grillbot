﻿using GrillBot.Data.Models.API;
using GrillBot.Database.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.CodeAnalysis;

namespace GrillBot.Tests.App.Controllers;

[ExcludeFromCodeCoverage]
public abstract class ControllerTest<TController> where TController : Controller
{
    protected TController Controller { get; set; }
    protected GrillBotContext DbContext { get; set; }

    protected abstract TController CreateController();

    [TestInitialize]
    public void Initialize()
    {
        Controller = CreateController();
    }

    public virtual void Cleanup() { }

    [TestCleanup]
    public void TestClean()
    {
        Cleanup();
        DbContext?.Dispose();
        Controller.Dispose();
    }

    protected void CheckResult<TResult>(IActionResult result) where TResult : IActionResult
    {
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(TResult));

        if (result is NotFoundObjectResult notFound)
            Assert.IsInstanceOfType(notFound.Value, typeof(MessageResponse));
    }

    protected void CheckResult<TResult, TOkModel>(ActionResult<TOkModel> result) where TResult : ObjectResult
    {
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result.Result, typeof(TResult));

        if (result.Result is OkObjectResult ok)
            Assert.IsInstanceOfType(ok.Value, typeof(TOkModel));
        else if (result.Result is NotFoundObjectResult notFound)
            Assert.IsInstanceOfType(notFound.Value, typeof(MessageResponse));
    }
}