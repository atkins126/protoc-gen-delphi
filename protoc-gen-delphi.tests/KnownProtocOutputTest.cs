/// Copyright 2020 Connor Roehricht (connor.work)
/// Copyright 2020 Sotax AG
/// 
/// Licensed under the Apache License, Version 2.0 (the "License");
/// you may not use this file except in compliance with the License.
/// You may obtain a copy of the License at
/// 
///     http://www.apache.org/licenses/LICENSE-2.0
/// 
/// Unless required by applicable law or agreed to in writing, software
/// distributed under the License is distributed on an "AS IS" BASIS,
/// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
/// See the License for the specific language governing permissions and
/// limitations under the License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace Work.Connor.Protobuf.Delphi.ProtocGenDelphi.Tests
{
    /// <summary>
    /// Tests <see cref="ProtocGenDelphi"/> as a plug-in to <c>protoc</c> with known inputs to <c>protoc</c> and outputs from the plug-in.
    /// </summary>
    public class KnownProtocOutputTest
    {
        /// <summary>
        /// Resource set of all test resource files for this kind of test
        /// </summary>
        private static readonly IResourceSet testResources = IResourceSet.Root.Nest("[known protoc output]");

        /// <summary>
        /// Resource set of all test resource files within folders containing expected plug-in output
        /// </summary>
        private static readonly IResourceSet allExpectedOutputFolderResources = testResources.Nest("[expected output folder]");

        /// <summary>
        /// Resource set of all test resource files within folders containing <c>protoc</c> input
        /// </summary>
        private static readonly IResourceSet allInputFolderResources = testResources.Nest("[input folder]");

        /// <summary>
        /// Resource set of all test resource files that are used as a single input protobuf schema definition file for <c>protoc</c>
        /// </summary>
        private static readonly IResourceSet allInputFileResources = testResources.Nest("[input schema file]");

        /// <summary>
        /// Names of all known test vectors
        /// </summary>
        /// <returns>Enumeration of test vector names</returns>
        private static IEnumerable<string> TestVectorNames() => allExpectedOutputFolderResources.GetIDs().WhereSuffixed(new Regex($"{Regex.Escape(".protoc-output")}/.*")).Distinct();

        /// <summary>
        /// Marker string in the name of of test vectors that indicates that the default runtime shall be used
        /// </summary>
        public static readonly string defaultRuntimeMarker = "with_default_runtime";

        /// <summary>
        /// Prefix to the name of .proto files in test input folders that shall be used as input files
        /// </summary>
        public static readonly string inputFilePrefix = "input_file_";

        /// <summary>
        /// Utility function to create a temporary scratch folder for testing.
        /// </summary>
        /// <returns>Path of the new folder</returns>
        private static string CreateScratchFolder()
        {
            string path = Path.GetTempFileName();
            File.Delete(path);
            Directory.CreateDirectory(path);
            return path;
        }

        /// <summary>
        /// Represents a test vector for this kind of test
        /// </summary>
        public class TestVector : IXunitSerializable
        {
            /// <summary>
            /// Name of the test vector
            /// </summary>
            private string name;

            /// <summary>
            /// Resource set of all test resource files that are used as <c>protoc</c> input for this test
            /// </summary>
            private IResourceSet inputFolderResources;

            /// <summary>
            /// Resource set of all test resource files that define expected plug-in output for this test
            /// </summary>
            private IResourceSet expectedOutputFolderResources;

            /// <summary>
            /// Folder containing the input file tree for <c>protoc</c>. Only present after test file tree setup.
            /// </summary>
            private string? inputFolder;

            /// <summary>
            /// Constructs a new test vector for deserialization by xUnit.
            /// </summary>
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable. -> Initialized during deserialization by xUnit or through InitializeResourceSet
            public TestVector() { }

            /// <summary>
            /// Constructs a new test vector.
            /// </summary>
            /// <param name="name">Name of the test vector</param>
            public TestVector(string name) 
            {
                this.name = name;
                InitializeResourceSets();
            }
#pragma warning restore CS8618

            private void InitializeResourceSets()
            {
                inputFolderResources = allInputFolderResources.Nest($"{name}.protoc-input/");
                expectedOutputFolderResources = allExpectedOutputFolderResources.Nest($"{name}.protoc-output/");
            }


            /// <summary>
            /// Name of the optional test resource file that is used as a single input protobuf schema definition file for <c>protoc</c> for this test
            /// </summary>
            private string InputSchemaFileName { get => $"{name}.proto"; }

            /// <summary>
            /// Name and contents of all .proto files that are used as <c>protoc</c> input for this test
            /// </summary>
            private IEnumerable<(string, string)> ProtoFilesToSetup { get => inputFolderResources.ReadAllResources().Concat(allInputFileResources.ReadResources(new[] { InputSchemaFileName })); }

            /// <summary>
            /// Names of all .proto files that shall be specified for generation in the <c>protoc</c> arguments
            /// </summary>
            private IEnumerable<string> InputProtoFileNames
            {
                get => inputFolderResources.GetIDs().Where(name => name.Contains(inputFilePrefix)).Concat(
                       allInputFileResources.GetIDs().Where(name => name.Equals(InputSchemaFileName)));
            }

            /// <summary>
            /// Creates a temporary file tree containing input files, required before using the test vector
            /// </summary>
            public void SetupFileTree()
            {
                inputFolder = CreateScratchFolder();
                foreach ((string name, string content) in ProtoFilesToSetup)
                {
                    string path = Path.Join(inputFolder, name);
                    Directory.CreateDirectory(Directory.GetParent(path).FullName);
                    File.WriteAllText(path, content);
                }
            }

            /// <summary>
            /// Arguments to <c>protoc</c>
            /// </summary>
            public IEnumerable<string> ProtocArgs { get => InputProtoFileNames.Prepend($"-I{inputFolder ?? throw new InvalidOperationException($"Test vector file tree was not setup using {nameof(SetupFileTree)}")}"); }

            /// <summary>
            /// Mapping of file paths of expected plug-in output files to their expected content
            /// </summary>
            public IDictionary<string, string> ExpectedOutputFiles
            {
                get {
                    Dictionary<string, string> files = new Dictionary<string, string>();
                    foreach (string expectedOutputFile in expectedOutputFolderResources.GetIDs())
                    {
                        string? content = expectedOutputFolderResources.ReadResource(expectedOutputFile);
                        if (content == null) throw new FileNotFoundException(expectedOutputFile);
                        files.Add(expectedOutputFile, content);
                    }
                    return files;
                }
            }

            /// <summary>
            /// <see langword="true"/> if the default runtime for <c>protoc-gen-delphi</c> shall be used instead of the stub runtime
            /// </summary>
            public bool ShallUseDefaultRuntime { get => name.Contains(defaultRuntimeMarker); }

            public void Deserialize(IXunitSerializationInfo info)
            {
                name = info.GetValue<string>(nameof(name));
                InitializeResourceSets();
            }

            public void Serialize(IXunitSerializationInfo info)
            {
                info.AddValue(nameof(name), name);
            }

            public override string? ToString() => name;
        }

        /// <summary>
        /// All known test vectors
        /// </summary>
        public static IEnumerable<object[]> TestVectors { get => TestVectorNames().Select(name => new object[] { new TestVector(name) }); }

        /// <summary>
        /// Constructs an executable file name for the current platform.
        /// </summary>
        /// <param name="name">The base name, without extension</param>
        /// <returns>The executable file name</returns>
        private static string GetExecutableName(string name)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return $"{name}.exe";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return name;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return name;
            throw new NotImplementedException("Unsupported OS");
        }

        /// <summary>
        /// Determines the platform identifier in <c>protoc</c>'s path.
        /// </summary>
        /// <returns>The <c>protoc</c> platform identifier string</returns>
        private static string GetProtocPlatform()
        {
            if (!Environment.Is64BitOperatingSystem) throw new NotImplementedException("Unsupported non-64-bit OS");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "windows_x64";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "macosx_x64";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "linux_x64";
            throw new NotImplementedException("Unsupported OS");
        }

        /// <summary>
        /// <see cref="ProtocGenDelphi"/> produces the expected output when used as a <c>protoc</c> plug-in.
        /// </summary>
        /// <param name="vector">Test vector</param>
        [Theory]
        [MemberData(nameof(TestVectors))]
        public void ProducesExpectedOutput(TestVector vector)
        {
            // Setup file tree as input for protoc, according to the test vector
            vector.SetupFileTree();
            // Create a scratch folder as output folder for the plug-in
            string outputFolder = CreateScratchFolder();
            // Run protoc
            using Process protoc = new Process();
            protoc.StartInfo.FileName = Path.Join("Google.Protobuf.Tools", "tools", GetProtocPlatform(), GetExecutableName("protoc"));
            // A leading dot seems to be required in the plugin folder name for protoc
            protoc.StartInfo.ArgumentList.Add($"--plugin={Path.Join(".", GetExecutableName("protoc-gen-delphi"))}");
            protoc.StartInfo.ArgumentList.Add($"--delphi_out={outputFolder}");
            if (!vector.ShallUseDefaultRuntime) protoc.StartInfo.ArgumentList.Add($"--delphi_opt={ProtocGenDelphi.customRuntimeOption}={IRuntimeSupport.Stub.DelphiNamespace}");
            foreach (string arg in vector.ProtocArgs) protoc.StartInfo.ArgumentList.Add(arg);
            protoc.StartInfo.CreateNoWindow = true;
            protoc.StartInfo.UseShellExecute = false;
            protoc.StartInfo.RedirectStandardError = true;
            string error = "";
            protoc.Start();
            protoc.ErrorDataReceived += delegate (object sender, DataReceivedEventArgs e) { error += e.Data; };
            protoc.BeginErrorReadLine();
            protoc.WaitForExit();
            // Check protoc success
            Assert.Equal(0, protoc.ExitCode);
            IDictionary<string, string> expectedOutputFiles = vector.ExpectedOutputFiles;
            // Check that expected files are generated
            foreach ((string path, string expectedContent) in expectedOutputFiles) Assert.Equal(expectedContent, File.ReadAllText(Path.Join(outputFolder, path)));
            // Check that no other files are generated
            foreach (string path in Directory.GetFiles(outputFolder, "*", SearchOption.AllDirectories)) Assert.Contains(Path.GetRelativePath(outputFolder, path).Replace('\\', '/'), expectedOutputFiles.Keys);
        }
    }
}
