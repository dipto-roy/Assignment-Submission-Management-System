using AssignmentSubmissionSystem.Application.Abstractions;
using AssignmentSubmissionSystem.Application.Classes;
using AssignmentSubmissionSystem.Application.Classes.Dtos;
using AssignmentSubmissionSystem.Application.Common.Exceptions;
using AssignmentSubmissionSystem.Domain.Entities;
using Moq;

namespace AssignmentSubmissionSystem.UnitTests.Classes;

public sealed class ClassServiceTests
{
    private readonly Mock<IClassRepository> _classRepository = new();
    private readonly ClassService _sut;

    public ClassServiceTests()
    {
        _sut = new ClassService(_classRepository.Object);
    }

    [Fact]
    public async Task CreateAsync_AddsClassWithGivenNameAndSection()
    {
        var dto = new CreateClassDto("Class 10", "A");

        var result = await _sut.CreateAsync(dto, CancellationToken.None);

        result.Name.Should().Be("Class 10");
        result.Section.Should().Be("A");
        _classRepository.Verify(r => r.AddAsync(It.IsAny<SchoolClass>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_Throws404_WhenClassDoesNotExist()
    {
        var id = Guid.NewGuid();
        _classRepository.Setup(r => r.FindByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((SchoolClass?)null);

        var act = () => _sut.UpdateAsync(id, new UpdateClassDto("X", null), CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundAppException>(act);
    }
}
