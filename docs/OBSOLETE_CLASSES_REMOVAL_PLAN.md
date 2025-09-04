# AutoTool �p�~�\��N���X�폜�v��

## ���ݔp�~�\��̃N���X

### 1. `Panels\ViewModel\EditPanelViewModel`
- **���**: `[Obsolete("Phase 3�œ����łɈڍs�BAutoTool.ViewModel.Panels.EditPanelViewModel���g�p���Ă�������", false)]`
- **�폜�\��**: v2.0.0
- **��փN���X**: `AutoTool.ViewModel.Panels.EditPanelViewModel`
- **�e���͈�**: MacroPanels�v���W�F�N�g�S��
- **�폜�菇**:
  1. �S�Q�Ƃ�V�����N���X�ɒu������
  2. �e�X�g�P�[�X�̍X�V
  3. �h�L�������g�X�V
  4. �N���X�폜

### 2. `Panels\ViewModel\ButtonPanelViewModel`
- **���**: `[Obsolete("Phase 3�œ����łɈڍs�BAutoTool.ViewModel.Panels.ButtonPanelViewModel���g�p���Ă�������", false)]`
- **�폜�\��**: v2.0.0
- **��փN���X**: `AutoTool.ViewModel.Panels.ButtonPanelViewModel`
- **�e���͈�**: UI��������
- **�폜�菇**: EditPanelViewModel�Ɠ��l

## �폜�X�P�W���[��

### Phase 1: �x������ (���� - v1.9.0)
- `[Obsolete]`�����ŃR���p�C�����x��
- �h�L�������g�ňڍs�K�C�h��
- �V�@�\�͓����ł݂̂ɒǉ�

### Phase 2: �G���[�� (v1.9.0 - v2.0.0)
```csharp
[Obsolete("���̃N���X�͍폜����܂����BAutoTool.ViewModel.Panels.EditPanelViewModel���g�p���Ă�������", true)]
```

### Phase 3: ���S�폜 (v2.0.0)
- �p�~�N���X�̕����폜
- �֘A�e�X�g�P�[�X�̍폜
- �v���W�F�N�g�t�@�C������̏���

## �������X�N���v�g

```powershell
# �p�~�N���X���o�X�N���v�g
Get-ChildItem -Recurse -Filter "*.cs" | Select-String "\[Obsolete" | ForEach-Object {
    Write-Host "�p�~�\��: $($_.Filename):$($_.LineNumber) - $($_.Line.Trim())"
}

# �Q�ƌ����X�N���v�g  
$obsoleteClasses = @("MacroPanels.ViewModel.EditPanelViewModel", "MacroPanels.ViewModel.ButtonPanelViewModel")
foreach ($class in $obsoleteClasses) {
    Get-ChildItem -Recurse -Filter "*.cs" | Select-String $class | ForEach-Object {
        Write-Host "�v�X�V: $($_.Filename):$($_.LineNumber) - $($_.Line.Trim())"
    }
}
```

## �ڍs�`�F�b�N���X�g

- [ ] �S`using`���̍X�V
- [ ] DI�R���e�i�o�^�̍X�V  
- [ ] XAML�o�C���f�B���O�̊m�F
- [ ] �P�̃e�X�g�̍X�V
- [ ] �����e�X�g�̎��s
- [ ] �p�t�H�[�}���X�e�X�g�̎��s
- [ ] �h�L�������g�̍X�V
- [ ] CHANGELOG.md�̍X�V

## ���X�N�]��

### �����X�N
- MainWindowViewModel�̓�������
- ���G�ȃv���L�V�v���p�e�B�̈ڍs

### �����X�N  
- XAML�o�C���f�B���O�̍X�V�R��
- DI�R���e�i�ݒ�̕s����

### �჊�X�N
- ���O�o�̓��b�Z�[�W�̕ύX
- �R�����g�╶���񃊃e����

## �ً}�����[���o�b�N�菇

1. Git�^�O�쐬�i�폜�O�j
2. �o�b�N�A�b�v�u�����`�쐬
3. ��蔭�����̑����߂��菇���쐬
4. �ڋq�ʒm�e���v���[�g����

## �폜������̌���

### �R�[�h�i������
- �d���R�[�h����: ��800�s�팸
- �ێ琫����: ���ꂳ�ꂽ�A�[�L�e�N�`��
- �e�X�g�e�Ր�: DI�Ή��ɂ�鍂���e�X�^�r���e�B

### �p�t�H�[�}���X����
- �������g�p�ʍ팸: �d���N���X����
- ���������ԒZ�k: �I�u�W�F�N�g�����̍œK��
- ���s���x����: �v���L�V�w�̍팸