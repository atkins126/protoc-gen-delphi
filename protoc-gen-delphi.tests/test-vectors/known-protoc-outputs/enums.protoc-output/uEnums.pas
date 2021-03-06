/// <remarks>
/// This unit corresponds to the protobuf schema definition (.proto file) <c>enums.proto</c>.
/// </remarks>
unit uEnums;

interface

uses
  Work.Connor.Protobuf.Delphi.ProtocGenDelphi.StubRuntime.uProtobufEnum;

type
  /// <remarks>
  /// This enumerated type corresponds to the protobuf enum <c>EnumX</c>.
  /// </remarks>
  TEnumX = (
    /// <remarks>
    /// This enumerated value corresponds to the protobuf enum constant <c>VALUE_X</c>.
    /// </remarks>
    EnumXValueX = 0
  )
  end;

type
  /// <remarks>
  /// This enumerated type corresponds to the protobuf enum <c>EnumY</c>.
  /// </remarks>
  TEnumY = (
    /// <remarks>
    /// This enumerated value corresponds to the protobuf enum constant <c>NONE</c>.
    /// </remarks>
    EnumYNone = 0,

    /// <remarks>
    /// This enumerated value corresponds to the protobuf enum constant <c>VALUE_Y</c>.
    /// </remarks>
    EnumYValueY = 3
  )
  end;

implementation

end.
