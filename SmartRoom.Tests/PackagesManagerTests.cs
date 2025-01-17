﻿using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.UITest;

namespace SmartRoom.Tests
{
    [TestFixture(Platform.Android)]
    public class PackagesManagerTests
    {
        private IApp app;
        private Platform platform;
        private SmartRoom.Emulator.Logic _logic;
        private Mock<Interfaces.ITcpConnector> _mocked;
        private Interfaces.IPackagesManager _manager;
        private System.Collections.ObjectModel.ObservableCollection<Models.SensorModel> _sensors;
        private System.Collections.ObjectModel.ObservableCollection<Models.SwitchModel> _switches;

        public PackagesManagerTests(Platform platform)
        {
            this.platform = platform;

            _mocked = new Mock<Interfaces.ITcpConnector>();
            _mocked.SetupGet(x => x.IsConnected).Returns(true);
            _mocked.SetupGet(x => x.IsReady).Returns(true);
            _mocked.Setup(x => x.Send(It.IsAny<byte[]>()))
                  .Raises<byte[]>(x => x.DataReceivedEvent += null, 
                                 (arr) => {
                                     var output = _logic.ProcessData(arr, arr.Length);
                                     return new Events.ObjectEventArgs(output.Take(output.Length - 1).ToList());
                                     });
        }

        [OneTimeSetUp]
        public void SetUp()
        {
            _manager = new Managers.PackagesManager(_mocked.Object);
        }

        [SetUp]
        public void Initialize()
        {
            _logic = new Emulator.Logic();
            _switches = new System.Collections.ObjectModel.ObservableCollection<Models.SwitchModel>()
            {
                new Models.ToggleSwitchModel(null, "D10", false, false),
                new Models.ToggleSwitchModel(null, "A1", false, true),
                new Models.SliderSwitchModel(null, "7", 0f, false),
                new Models.SliderSwitchModel(null, "8", 0f, true),
                new Models.ColorSwitchModel(null, "0", "1", "2", new Models.ColorModel(0,0,0), false),
                new Models.ColorSwitchModel(null, "D0", "D1", "D2", new Models.ColorModel(0,0,0), true)
            };
            _sensors = new System.Collections.ObjectModel.ObservableCollection<Models.SensorModel>()
            {
                new Models.ValueSensorModel(null, "0", true),
                new Models.TextSensorModel(null, "1", true),
                new Models.TextSensorModel(null, "2", true),
                new Models.ValueSensorModel(null, "0", false),
                new Models.ValueSensorModel(null, "D1", false),
                new Models.ValueSensorModel(null, "A9", false)
            };

            foreach (var s in _switches)
                _manager.PinValuesUpdated += s.PinUpdateListener;
            foreach (var s in _sensors)
                if (s.IsId) _manager.IdValuesReceived += s.UpdateListener;
                else _manager.PinValuesUpdated += s.UpdateListener;
        }

        [TestCase("D10", 255)]
        [TestCase("7", 204)]
        [TestCase("A0", 128)]
        public void SetSingleValuesNoFade(string pin, byte value)
        {
            //Act
            _manager.SetValue(pin, value, false);

            //Assert
            Assert.IsTrue(_logic.WasFading(pin) == false);
            Assert.AreEqual(value, _logic.Pins[pin]);
        }

        [TestCase("D10", 255)]
        [TestCase("7", 204)]
        [TestCase("A0", 128)]
        public void SetSingleValuesFade(string pin, byte value)
        {
            //Act
            _manager.SetValue(pin, value, true);

            //Assert
            Assert.IsTrue(_logic.WasFading(pin));
        }

        [Test]
        public void SetSingleModelNoFade()
        {
            //Arrange
            var m1 = new Models.ToggleSwitchModel(null, "D10", true, false);
            var m2 = new Models.SliderSwitchModel(null, "7", 0.8f, false);
            var m3 = new Models.ColorSwitchModel(null, "0", "1", "2", new Models.ColorModel(255, 128, 96), false);

            //Act
            _manager.SetValue(m1);
            _manager.SetValue(m2);
            _manager.SetValue(m3);

            //Assert
            Assert.IsTrue(_logic.WasFading("D10") == false);
            Assert.IsTrue(_logic.WasFading("7") == false);
            Assert.IsTrue(_logic.WasFading("0") == false);
            Assert.IsTrue(_logic.WasFading("1") == false);
            Assert.IsTrue(_logic.WasFading("2") == false);
            Assert.AreEqual((byte)255, _logic.Pins["D10"]);
            Assert.AreEqual((byte)204, _logic.Pins["7"]);
            Assert.AreEqual((byte)255, _logic.Pins["0"]);
            Assert.AreEqual((byte)128, _logic.Pins["1"]);
            Assert.AreEqual((byte)96, _logic.Pins["2"]);
        }

        [Test]
        public void SetSingleModelFade()
        {
            //Arrange
            var m1 = new Models.ToggleSwitchModel(null, "D10", true, true);
            var m2 = new Models.SliderSwitchModel(null, "7", 0.8f, true);
            var m3 = new Models.ColorSwitchModel(null, "0", "1", "2", new Models.ColorModel(255, 128, 96), true);

            //Act
            _manager.SetValue(m1);
            _manager.SetValue(m2);
            _manager.SetValue(m3);

            //Assert
            Assert.IsTrue(_logic.WasFading("D10"));
            Assert.IsTrue(_logic.WasFading("7"));
            Assert.IsTrue(_logic.WasFading("0"));
            Assert.IsTrue(_logic.WasFading("1"));
            Assert.IsTrue(_logic.WasFading("2"));
        }

        [Test]
        public void SetMultipleModelNoFade()
        {
            //Arrange
            var list = new List<Models.SwitchModel>()
            {
                new Models.ToggleSwitchModel(null, "D10", true, false),
                new Models.SliderSwitchModel(null, "7", 0.8f, false),
                new Models.ColorSwitchModel(null, "0", "1", "2", new Models.ColorModel(255, 128, 96), false)
            };

            //Act
            _manager.SetValue(list);

            //Assert
            Assert.IsTrue(_logic.WasFading("D10") == false);
            Assert.IsTrue(_logic.WasFading("7") == false);
            Assert.IsTrue(_logic.WasFading("0") == false);
            Assert.IsTrue(_logic.WasFading("1") == false);
            Assert.IsTrue(_logic.WasFading("2") == false);
            Assert.AreEqual((byte)255, _logic.Pins["D10"]);
            Assert.AreEqual((byte)204, _logic.Pins["7"]);
            Assert.AreEqual((byte)255, _logic.Pins["0"]);
            Assert.AreEqual((byte)128, _logic.Pins["1"]);
            Assert.AreEqual((byte)96, _logic.Pins["2"]);
        }

        [Test]
        public void SetMultipleModelFade()
        {
            //Arrange
            var list = new List<Models.SwitchModel>()
            {
                new Models.ToggleSwitchModel(null, "D10", true, true),
                new Models.SliderSwitchModel(null, "7", 0.8f, true),
                new Models.ColorSwitchModel(null, "0", "1", "2", new Models.ColorModel(255, 128, 96), true)
            };

            //Act
            _manager.SetValue(list);

            //Assert
            Assert.IsTrue(_logic.WasFading("D10"));
            Assert.IsTrue(_logic.WasFading("7"));
            Assert.IsTrue(_logic.WasFading("0"));
            Assert.IsTrue(_logic.WasFading("1"));
            Assert.IsTrue(_logic.WasFading("2"));
        }


        [TestCase("D10", 255)]
        [TestCase("7", 204)]
        [TestCase("A1", 255)]
        [TestCase("D0", 188)]
        public void GetSinglePinValue(string pin, byte value)
        {
            //Arrange
            var are = new AutoResetEvent(false);
            _logic.Pins[pin] = value;
            _manager.PinValuesUpdated += delegate { are.Set(); };

            //Act
            _manager.GetValue(pin);

            //Arrange
            Assert.IsTrue(are.WaitOne(1000));
            bool found = _switches.SelectMany(x => x.GetPinsValue())
                                   .Where(x => x.Item1 == pin)
                                   .All(x => x.Item2 == value);
            Assert.IsTrue(found);
        }

        [Test]
        public void GetSingleModelValue()
        {
            //Arrange
            var are = new AutoResetEvent(false);
            _logic.Pins["D10"] = 255;
            _logic.Pins["7"] = 204;
            _logic.Pins["0"] = 255;
            _logic.Pins["1"] = 128;
            _logic.Pins["2"] = 96;

            var m1 = new Models.ToggleSwitchModel(null, "D10", true, false, true);
            var m2 = new Models.SliderSwitchModel(null, "7", 0.8f, false, true);
            var m3 = new Models.ColorSwitchModel(null, "0", "1", "2", new Models.ColorModel(255, 128, 96), false, true);

            int counter = 0;
            _manager.PinValuesUpdated += delegate { counter++; if (counter == 5) are.Set(); };

            //Act
            _manager.GetValue(m1);
            _manager.GetValue(m2);
            _manager.GetValue(m3);

            //Arrange
            Assert.IsTrue(are.WaitOne(1000), $"{counter}");
            Assert.Contains(m1, _switches);
            Assert.Contains(m2, _switches);
            Assert.Contains(m3, _switches);
        }

        [Test]
        public void GetMultipleModelValue()
        {
            //Arrange
            var are = new AutoResetEvent(false);
            _logic.Pins["D10"] = 255;
            _logic.Pins["7"] = 204;
            _logic.Pins["0"] = 255;
            _logic.Pins["1"] = 128;
            _logic.Pins["2"] = 96;

            var expected = new List<Models.SwitchModel>()
            {
                new Models.ToggleSwitchModel(null, "D10", true, false, true),
                new Models.SliderSwitchModel(null, "7", 0.8f, false, true),
                new Models.ColorSwitchModel(null, "0", "1", "2", new Models.ColorModel(255, 128, 96), false, true)
            };

            int counter = 0;
            _manager.PinValuesUpdated += delegate { counter++; if (counter == 5) are.Set(); };

            //Act
            _manager.GetValue(expected);

            //Arrange
            Assert.IsTrue(are.WaitOne(1000));
            foreach (var e in expected)
                Assert.Contains(e, _switches);
        }

        [Test]
        public void GetSingleIdId()
        {
            //Arrange
            var are = new AutoResetEvent(false);
            _logic.IdValues["0"] = new Tuple<bool, string>(false, "255");
            _logic.IdValues["1"] = new Tuple<bool, string>(true, "Hello, World!");

            int counter = 0;
            _manager.IdValuesReceived += delegate { counter++; if (counter == 2) are.Set(); };

            //Act
            _manager.GetId("0");
            _manager.GetId("1");

            //Arrange
            Assert.IsTrue(are.WaitOne());
            Assert.IsTrue((_sensors[0] as Models.ValueSensorModel).Value == 255);
            Assert.IsTrue((_sensors[1] as Models.TextSensorModel).Text == "Hello, World!");
        }

        [Test]
        public void GetMultipleIdId()
        {
            //Arrange
            var are = new AutoResetEvent(false);
            _logic.IdValues["0"] = new Tuple<bool, string>(false, "255");
            _logic.IdValues["1"] = new Tuple<bool, string>(true, "Hello, World!");

            var input = new List<string> { "0", "1" };

            int counter = 0;
            _manager.IdValuesReceived += delegate { counter++; if (counter == 2) are.Set(); };

            //Act
            _manager.GetId(input);

            //Arrange
            Assert.IsTrue(are.WaitOne());
            Assert.IsTrue((_sensors[0] as Models.ValueSensorModel).Value == 255);
            Assert.IsTrue((_sensors[1] as Models.TextSensorModel).Text == "Hello, World!");
        }

        [Test]
        public void GetSingleModelData()
        {
            //Arrange
            var are = new AutoResetEvent(false);
            _logic.IdValues["0"] = new Tuple<bool, string>(false, "255");
            _logic.IdValues["1"] = new Tuple<bool, string>(true, "Hello, World!");
            _logic.IdValues["2"] = new Tuple<bool, string>(true, "18");
            _logic.Pins["0"] = 67;
            _logic.Pins["D1"] = 10;
            _logic.Pins["A9"] = 255;

            var expected = new List<Models.SensorModel>()
            {
                new Models.ValueSensorModel(null, "0", true) { Value = 255 },
                new Models.TextSensorModel(null, "1", true) { Text = "Hello, World!" },
                new Models.TextSensorModel(null, "2", true) { Text = "18" },
                new Models.ValueSensorModel(null, "0", false) { Value = 67 },
                new Models.ValueSensorModel(null, "D1", false) {Value = 10 },
                new Models.ValueSensorModel(null, "A9", false) { Value = 255 }
            };

            int counter = 0;
            _manager.IdValuesReceived += delegate { counter++; if (counter == 6) are.Set(); };
            _manager.PinValuesUpdated += delegate { counter++; if (counter == 6) are.Set(); };

            //Act
            foreach (var s in _sensors)
                _manager.GetData(s);

            //Arrange
            Assert.IsTrue(are.WaitOne());
            foreach (var e in expected)
                Assert.Contains(e, _sensors);
        }

        [Test]
        public void GetMultipleModelData()
        {
            //Arrange
            var are = new AutoResetEvent(false);
            _logic.IdValues["0"] = new Tuple<bool, string>(false, "255");
            _logic.IdValues["1"] = new Tuple<bool, string>(true, "Hello, World!");
            _logic.IdValues["2"] = new Tuple<bool, string>(true, "18");
            _logic.Pins["0"] = 67;
            _logic.Pins["D1"] = 10;
            _logic.Pins["A9"] = 255;

            var expected = new List<Models.SensorModel>()
            {
                new Models.ValueSensorModel(null, "0", true) { Value = 255 },
                new Models.TextSensorModel(null, "1", true) { Text = "Hello, World!" },
                new Models.TextSensorModel(null, "2", true) { Text = "18" },
                new Models.ValueSensorModel(null, "0", false) { Value = 67 },
                new Models.ValueSensorModel(null, "D1", false) {Value = 10 },
                new Models.ValueSensorModel(null, "A9", false) { Value = 255 }
            };

            int counter = 0;
            _manager.IdValuesReceived += delegate { counter++; if (counter == 6) are.Set(); };
            _manager.PinValuesUpdated += delegate { counter++; if (counter == 6) are.Set(); };

            //Act
            _manager.GetData(_sensors);

            //Arrange
            Assert.IsTrue(are.WaitOne());
            foreach (var e in expected)
                Assert.Contains(e, _sensors);
        }

        [Test]
        public void MutlipleMultithread()
        {
            //Arrange
            var t1 = new Task(() =>
            {
                _manager.SetValue("0", 213, false);
                _manager.SetValue("1", 67, false);
                _manager.SetValue("2", 16, false);
                _manager.SetValue("3", 9, false);
            });
            var t2 = new Task(() =>
            {
                _manager.GetValue("0");
                _manager.GetValue("1");
                _manager.GetValue("2");
                _manager.GetValue("3");
            });
            var t3 = new Task(() =>
            {
                _manager.SetValue("D0", 213, false);
                _manager.SetValue("A1", 67, false);
                _manager.SetValue("D2", 16, false);
                _manager.SetValue("A3", 9, false);
            });
            var t4 = new Task(() =>
            {
                _manager.GetValue("10");
                _manager.GetValue("11");
                _manager.GetValue("12");
                _manager.GetValue("13");
            });

            //Act
            t1.Start();
            t2.Start();
            t3.Start();
            t4.Start();

            //Assert
            Task.WaitAll(t1, t2, t3, t4);
            Assert.AreEqual((byte)213, _logic.Pins["0"]);
            Assert.AreEqual((byte)67, _logic.Pins["1"]);
            Assert.AreEqual((byte)16, _logic.Pins["2"]);
            Assert.AreEqual((byte)9, _logic.Pins["3"]);
            Assert.AreEqual((byte)213, _logic.Pins["D0"]);
            Assert.AreEqual((byte)67, _logic.Pins["A1"]);
            Assert.AreEqual((byte)16, _logic.Pins["D2"]);
            Assert.AreEqual((byte)9, _logic.Pins["A3"]);
        }
    }
}
