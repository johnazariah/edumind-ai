# DevContainer Python & Jupyter Setup

**Date:** October 19, 2025  
**Purpose:** Enable Python notebooks for Azure deployment automation

## 🎯 Changes Made

### 1. Updated `.devcontainer/devcontainer.json`

#### Added Python Feature

```json
"ghcr.io/devcontainers/features/python:1": {
  "version": "3.11",
  "installTools": true,
  "optimize": true
}
```

#### Added VS Code Extensions

- `ms-toolsai.jupyter` - Jupyter notebook support
- `ms-python.python` - Python language support  
- `ms-python.vscode-pylance` - Python language server

### 2. Updated `.devcontainer/post-create.sh`

Added Python package installation:

```bash
# Verify Python installation
echo "✅ Verifying Python..."
python3 --version
pip3 --version

# Install Python packages for Jupyter notebooks
echo "📦 Installing Python packages for notebooks..."
pip3 install --user ipykernel ipython jupyter nbformat nbclient
```

## 📦 What Gets Installed

### Python Environment

- Python 3.11
- pip (Python package manager)
- setuptools & wheel

### Jupyter Stack

- `ipykernel` - Jupyter kernel for Python
- `ipython` - Enhanced Python REPL
- `jupyter` - Jupyter notebook server
- `nbformat` - Notebook file format tools
- `nbclient` - Client for executing notebooks

## 🚀 Usage

### After Container Rebuild

The devcontainer will automatically:

1. Install Python 3.11
2. Install pip
3. Install Jupyter and ipykernel
4. Configure VS Code for notebooks

### Running the Deployment Notebook

1. Open `docs/deployment/azure-deployment.ipynb`
2. Select Python kernel (3.11)
3. Run cells sequentially
4. Monitor Azure deployment progress

### Manual Package Installation (if needed)

```bash
# Install additional packages
pip3 install --user package-name

# List installed packages
pip3 list

# Upgrade a package
pip3 install --user --upgrade package-name
```

## 🔧 Troubleshooting

### Issue: Kernel not found

**Solution:**

```bash
python3 -m ipykernel install --user --name python3 --display-name "Python 3.11"
```

### Issue: Module not found

**Solution:**

```bash
pip3 install --user module-name
```

### Issue: Permissions error

**Solution:**  
Always use `--user` flag with pip to install in user space

## ✅ Verification Commands

```bash
# Check Python version
python3 --version

# Check pip version
pip3 --version

# Verify ipykernel
python3 -m ipykernel --version

# List installed kernels
jupyter kernelspec list

# List installed packages
pip3 list | grep -E "ipykernel|ipython|jupyter"
```

Expected output:

```
Python 3.11.x
pip 23.x.x
ipykernel          6.x.x
ipython            8.x.x
jupyter            1.x.x
jupyter-client     8.x.x
jupyter-core       5.x.x
nbclient           0.x.x
nbformat           5.x.x
```

## 📊 Benefits

### Before

- ❌ No Python support
- ❌ Notebooks wouldn't run
- ❌ Manual installation required
- ❌ Inconsistent environments

### After

- ✅ Python 3.11 pre-installed
- ✅ Jupyter ready out-of-box
- ✅ Automatic setup
- ✅ Consistent dev environment

## 🎓 Next Steps

1. **Rebuild Container**
   - Command Palette: "Dev Containers: Rebuild Container"
   - OR restart VS Code

2. **Open Deployment Notebook**
   - Navigate to `docs/deployment/azure-deployment.ipynb`
   - Select Python 3.11 kernel
   - Begin deployment

3. **Run Deployment**
   - Execute cells sequentially
   - Follow prompts
   - Monitor progress

## 📝 Files Modified

1. `.devcontainer/devcontainer.json`
   - Added Python feature
   - Added Jupyter/Python extensions

2. `.devcontainer/post-create.sh`
   - Added Python verification
   - Added pip package installation
   - Updated tool summary

## 🔒 Security Notes

- Packages installed with `--user` flag (user-level, not system)
- No sudo required for Python packages
- Isolated from system Python
- Each user has their own packages

## 📚 Documentation

- [Python Feature](https://github.com/devcontainers/features/tree/main/src/python)
- [Jupyter Extension](https://marketplace.visualstudio.com/items?itemName=ms-toolsai.jupyter)
- [Python Extension](https://marketplace.visualstudio.com/items?itemName=ms-python.python)
- [ipykernel Docs](https://ipykernel.readthedocs.io/)

---

**Status:** Ready for container rebuild  
**Next Action:** Rebuild devcontainer to apply changes
